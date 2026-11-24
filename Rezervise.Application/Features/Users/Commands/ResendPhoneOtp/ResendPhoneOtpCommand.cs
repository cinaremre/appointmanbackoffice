using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.ResendPhoneOtp
{
    /// <summary>
    /// Telefon doğrulama kodunu yeniden gönderir. Yanıt, numaranın sistemde
    /// kayıtlı olup olmadığını sızdırmamak için her durumda başarılı döner.
    /// </summary>
    public class ResendPhoneOtpCommand : IRequest<Result<bool>>
    {
        /// <summary>Kodun gönderileceği telefon numarası.</summary>
        public string PhoneNumber { get; set; }
    }

    /// <summary>
    /// Telefon doğrulama kodunu yeniden gönderir. Numaranın kayıtlı olup olmadığını
    /// sızdırmamak için bilinmeyen/zaten doğrulanmış numaralarda sessizce başarılı
    /// döner. Bekleme süresi (varsayılan 180 saniye, <c>Otp:ResendCooldownSeconds</c>)
    /// dolmadan gelen istekte yeni kod üretilmez ve kalan saniyeyi taşıyan
    /// <c>TOO_MANY_REQUESTS</c> hatası döner.
    /// </summary>
    public class ResendPhoneOtpCommandHandler : IRequestHandler<ResendPhoneOtpCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpCodeService _otpCodeService;
        private readonly ISmsCodeSender _smsCodeSender;
        private readonly IDateTimeService _dateTimeService;
        private readonly OtpOptions _otpOptions;

        public ResendPhoneOtpCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IOtpCodeService otpCodeService,
            ISmsCodeSender smsCodeSender,
            IDateTimeService dateTimeService,
            OtpOptions otpOptions)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _otpCodeService = otpCodeService;
            _smsCodeSender = smsCodeSender;
            _dateTimeService = dateTimeService;
            _otpOptions = otpOptions;
        }

        public async Task<Result<bool>> Handle(
            ResendPhoneOtpCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    return Result<bool>.Success(true);

                var phone = new PhoneNumber(request.PhoneNumber);

                var users = await _userRepository.FindAsync(
                    u => u.PhoneNumber == phone && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                // Numara kayıtlı değil veya zaten doğrulanmış: bilgi sızdırılmaz.
                if (user is null || user.IsPhoneVerified)
                    return Result<bool>.Success(true);

                var utcNow = _dateTimeService.UtcNow;
                var cooldown = TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds);

                // Bekleme süresi dolmadıysa yeni kod üretilmez; istemcinin geri
                // sayım gösterebilmesi için kalan saniye hata ile döner.
                if (user.PhoneVerificationRequestedAt.HasValue)
                {
                    var elapsed = utcNow - user.PhoneVerificationRequestedAt.Value;

                    if (elapsed < cooldown)
                    {
                        var remainingSeconds = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);

                        return Result<bool>.Failure(Error.TooManyRequests(
                            "Otp.ResendCooldown", remainingSeconds, remainingSeconds));
                    }
                }

                var code = _otpCodeService.IssueCode(user, utcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // SMS hatası akışı bozmaz; kullanıcı tekrar isteyebilir.
                await _smsCodeSender.SendAsync(user.PhoneNumber.Value, code, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception)
            {
                return Result<bool>.Failure(
                    Error.Internal("Doğrulama kodu yeniden gönderilirken bir hata oluştu."));
            }
        }
    }
}
