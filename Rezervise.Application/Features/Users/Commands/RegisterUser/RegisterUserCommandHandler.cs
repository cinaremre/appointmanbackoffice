using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Rezervise.Application.Features.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RezerviseMetrics _metrics;
        private readonly IEmailQueue _emailQueue;
        private readonly EmailOptions _emailOptions;
        private readonly ILocalizationService _localizationService;
        private readonly IOtpCodeService _otpCodeService;
        private readonly ISmsCodeSender _smsCodeSender;
        private readonly IDateTimeService _dateTimeService;

        public RegisterUserCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            RezerviseMetrics metrics,
            IEmailQueue emailQueue,
            EmailOptions emailOptions,
            ILocalizationService localizationService,
            IOtpCodeService otpCodeService,
            ISmsCodeSender smsCodeSender,
            IDateTimeService dateTimeService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _metrics = metrics;
            _emailQueue = emailQueue;
            _emailOptions = emailOptions;
            _localizationService = localizationService;
            _otpCodeService = otpCodeService;
            _smsCodeSender = smsCodeSender;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Email kontrolü
                //    Müşteri rolünde (role 0) e-posta boş/null geldiğinde User.Email null
                //    olarak kalır; diğer rollerde validator zaten zorunluluk gerektirir.
                var email = request.Role == EnumUserRole.Customer && string.IsNullOrWhiteSpace(request.Email)
                    ? null
                    : new Email(request.Email!);
                var existingUserByEmail = await _userRepository.FindAsync(
                    u => u.Email == email && !u.IsDeleted,
                    cancellationToken);

                if (existingUserByEmail.Any())
                    return Result<Guid>.Failure(Error.Conflict("User.EmailTaken"));

                // 2. Telefon kontrolü
                var phone = new PhoneNumber(request.PhoneNumber);
                var existingUserByPhone = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                if (existingUserByPhone.Any())
                    return Result<Guid>.Failure(Error.Conflict("User.PhoneTaken"));

                // 3. Yetkilendirme: istemci yalnızca self-assignable rolleri talep edebilir.
                //    Admin / SuperAdmin / Moderator gibi roller yalnızca yönetici tarafından atanır.
                if (!SelfAssignableRoles.IsSelfAssignable(request.Role))
                    return Result<Guid>.Failure(Error.Forbidden("Validation.RoleNotSelfAssignable"));

                // 4. Domain entity oluştur
                var user = new User(
                    request.FirstName,
                    request.LastName,
                    email,
                    phone,
                    request.Role);

                // 4. Şifre haşhle (BCrypt)
                var passwordHash = BCryptNet.HashPassword(request.Password);
                user.SetPasswordHash(passwordHash);

                if (request.DateOfBirth.HasValue)
                {
                    user.SetDateOfBirth(request.DateOfBirth);
                }

                // 5. Konum bilgisi (il / ilçe) - aramalarda varsayılan konum olarak kullanılır
                if (request.CityId.HasValue || request.DistrictId.HasValue)
                {
                    user.UpdateLocation(request.CityId, request.DistrictId);
                }

                // 6. Dil tercihi - boş gelirse domain varsayılanı ("tr") korunur
                if (!string.IsNullOrWhiteSpace(request.LanguageCode))
                {
                    user.SetLanguageCode(request.LanguageCode);
                }

                // 7. Kaydet
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 8. E-posta doğrulama token'i. Ham değer yalnızca e-postada yer alır;
                //    veritabanında SHA-256 hash'i saklanır ve token tek kullanımlıktır.
                var rawVerificationToken = Guid.NewGuid().ToString("N");

                user.IssueEmailVerificationToken(
                    RefreshTokenHasher.Hash(rawVerificationToken),
                    DateTime.UtcNow.AddHours(_emailOptions.TokenLifetimeHours),
                    DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _metrics.RecordUserRegistered(user.Role.ToString());

                // 9. Doğrulama e-postası kuyruğa verilir; gönderim arka planda yapılır.
                //    Gönderim hatası kaydı başarısız göstermemelidir.
                TryQueueVerificationEmail(user, rawVerificationToken);

                // 10. Telefon doğrulama kodu üretilir ve gönderilir. OTP doğrulandığında
                //     kullanıcı aktifleşir ve giriş yapabilir (e-posta doğrulaması korunur).
                await TrySendPhoneVerificationCode(user, cancellationToken);

                return Result<Guid>.Success(user.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanlı iki kayıt denemesinde ikincisi unique index'e takılır.
                return Result<Guid>.Failure(ConcurrencyErrors.DuplicateRegistration());
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("User.RegisterFailed", ex.Message));
            }
        }

        /// <summary>
        /// Telefon doğrulama kodunu üretir ve SMS hattına bırakır. Gönderim veya
        /// yapılandırma hatası kayıt akışını bozmaz; kullanıcı kodu yeniden isteyebilir.
        /// </summary>
        /// <param name="user">Kodu alacak kullanıcı.</param>
        /// <param name="cancellationToken">İptal token'i.</param>
        private async Task TrySendPhoneVerificationCode(User user, CancellationToken cancellationToken)
        {
            try
            {
                var code = _otpCodeService.IssueCode(user, _dateTimeService.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _smsCodeSender.SendAsync(user.PhoneNumber.Value, code, cancellationToken);
            }
            catch (Exception)
            {
                // SMS/OTP hatası kullanıcı oluşturmayı geri almaz.
            }
        }

        /// <summary>
        /// Doğrulama e-postasını kuyruğa bırakır. Kuyruk veya şablon hatası
        /// kayıt akışını bozmaz: kullanıcı daha sonra yeniden gönderim isteyebilir.
        /// </summary>
        /// <param name="user">Doğrulanacak kullanıcı.</param>
        /// <param name="rawToken">E-postaya yazılacak ham (hash'lenmemiş) token.</param>
        private void TryQueueVerificationEmail(User user, string rawToken)
        {
            // E-postası olmayan kullanıcılar için doğrulama e-postası gönderilmez.
            if (user.Email is null)
                return;

            if (string.IsNullOrWhiteSpace(_emailOptions.VerificationLinkBaseUrl))
                return;

            try
            {
                var link = EmailVerificationLinkBuilder.Build(_emailOptions.VerificationLinkBaseUrl, rawToken);

                _emailQueue.TryEnqueue(new EmailMessage
                {
                    To = user.Email,
                    Subject = _localizationService.Get("Email.VerifySubject"),
                    HtmlBody = _localizationService.Get("Email.VerifyBody", link)
                });
            }
            catch (Exception)
            {
                // E-posta hatası kullanıcı oluşturmayı geri almaz; idempotent yeniden gönderim vardır.
            }
        }
    }
}