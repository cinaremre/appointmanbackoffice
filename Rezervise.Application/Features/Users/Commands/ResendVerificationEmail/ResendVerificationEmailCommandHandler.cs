using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Common.Security;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Doğrulama e-postasını yeniden gönderir. Adresin sistemde kayıtlı olup
    /// olmadığını sızdırmamak için bilinmeyen/zaten doğrulanmış adreslerde sessizce
    /// başarılı döner. Bekleme süresi (varsayılan 3 dakika, <c>Email:ResendCooldownMinutes</c>)
    /// dolmadan gelen istekte e-posta gönderilmez ve kalan saniyeyi taşıyan
    /// <c>TOO_MANY_REQUESTS</c> hatası döner.
    /// </summary>
    public class ResendVerificationEmailCommandHandler
        : IRequestHandler<ResendVerificationEmailCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailQueue _emailQueue;
        private readonly EmailOptions _emailOptions;
        private readonly ILocalizationService _localizationService;
        private readonly IDateTimeService _dateTimeService;

        /// <param name="userRepository">Kullanıcı deposu.</param>
        /// <param name="unitOfWork">Kalıcılık birimi.</param>
        /// <param name="emailQueue">Arka plan e-posta kuyruğu.</param>
        /// <param name="emailOptions">Bağ adresi ve bekleme süresi ayarları.</param>
        /// <param name="localizationService">Konu/gövde metinlerini çözer.</param>
        /// <param name="dateTimeService">UTC saat (yeniden gönderim sınırı için).</param>
        public ResendVerificationEmailCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IEmailQueue emailQueue,
            EmailOptions emailOptions,
            ILocalizationService localizationService,
            IDateTimeService dateTimeService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailQueue = emailQueue;
            _emailOptions = emailOptions;
            _localizationService = localizationService;
            _dateTimeService = dateTimeService;
        }

        /// <param name="request">E-posta adresini taşıyan komut.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <returns>Gönderim yapıldıysa başarılı; bekleme süresi dolmadıysa kalan saniyeli hata.</returns>
        public async Task<Result<bool>> Handle(
            ResendVerificationEmailCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(email))
                    return Result<bool>.Success(true);

                var users = await _userRepository.FindAsync(
                    u => u.Email == email && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                // Hesap yok veya zaten doğrulanmış: bilgi sızdırmadan başarılı dönülür.
                if (user is null || user.IsEmailVerified)
                    return Result<bool>.Success(true);

                var utcNow = _dateTimeService.UtcNow;
                var cooldown = TimeSpan.FromMinutes(_emailOptions.ResendCooldownMinutes);

                // Sık gönderimi engelle: bekleme süresi dolmadıysa yeni token
                // üretilmez; istemcinin geri sayım gösterebilmesi için kalan saniye döner.
                if (user.EmailVerificationRequestedAt.HasValue)
                {
                    var elapsed = utcNow - user.EmailVerificationRequestedAt.Value;

                    if (elapsed < cooldown)
                    {
                        var remainingSeconds = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);

                        return Result<bool>.Failure(Error.TooManyRequests(
                            "Auth.ResendVerificationCooldown", remainingSeconds, remainingSeconds));
                    }
                }

                var rawToken = Guid.NewGuid().ToString("N");

                user.IssueEmailVerificationToken(
                    RefreshTokenHasher.Hash(rawToken),
                    utcNow.AddHours(_emailOptions.TokenLifetimeHours),
                    utcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                TryQueueVerificationEmail(user, rawToken);

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    Error.Internal("Doğrulama e-postası gönderilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Doğrulama e-postasını kuyruğa bırakır; gönderim hatası yanıtı bozmaz.
        /// </summary>
        /// <param name="user">Doğrulanacak kullanıcı.</param>
        /// <param name="rawToken">E-postaya yazılacak ham token.</param>
        private void TryQueueVerificationEmail(User user, string rawToken)
        {
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
                // Kuyruk hatası yeniden gönderim akışını bozmaz.
            }
        }
    }
}
