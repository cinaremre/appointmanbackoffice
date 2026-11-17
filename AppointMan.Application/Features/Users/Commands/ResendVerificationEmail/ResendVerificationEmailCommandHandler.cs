using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Models;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Commands.ResendVerificationEmail
{
    /// <summary>
    /// Dogrulama e-postasini yeniden gonderir. Yanit, adresin sistemde kayitli
    /// olup olmadigini sizdirmamak icin her durumda basarili doner.
    /// </summary>
    public class ResendVerificationEmailCommandHandler
        : IRequestHandler<ResendVerificationEmailCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailQueue _emailQueue;
        private readonly EmailOptions _emailOptions;
        private readonly ILocalizationService _localizationService;

        /// <param name="userRepository">Kullanici deposu.</param>
        /// <param name="unitOfWork">Kalicilik birimi.</param>
        /// <param name="emailQueue">Arka plan e-posta kuyrugu.</param>
        /// <param name="emailOptions">Bag adresi ve bekleme suresi ayarlari.</param>
        /// <param name="localizationService">Konu/govde metinlerini cozer.</param>
        public ResendVerificationEmailCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            IEmailQueue emailQueue,
            EmailOptions emailOptions,
            ILocalizationService localizationService)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _emailQueue = emailQueue;
            _emailOptions = emailOptions;
            _localizationService = localizationService;
        }

        /// <param name="request">E-posta adresini tasiyan komut.</param>
        /// <param name="cancellationToken">Iptal belirteci.</param>
        /// <returns>Her durumda <c>true</c>.</returns>
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

                // Hesap yok veya zaten dogrulanmis: bilgi sizdirmadan basarili donulur.
                if (user is null || user.IsEmailVerified)
                    return Result<bool>.Success(true);

                // 5. Sik gonderimi engelle: bekleme suresi dolmadiysa yeni token uretilmez.
                if (user.EmailVerificationRequestedAt.HasValue &&
                    DateTime.UtcNow - user.EmailVerificationRequestedAt.Value
                        < TimeSpan.FromMinutes(_emailOptions.ResendCooldownMinutes))
                {
                    return Result<bool>.Success(true);
                }

                var rawToken = Guid.NewGuid().ToString("N");

                user.IssueEmailVerificationToken(
                    RefreshTokenHasher.Hash(rawToken),
                    DateTime.UtcNow.AddHours(_emailOptions.TokenLifetimeHours),
                    DateTime.UtcNow);

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
                    Error.Internal("Dogrulama e-postasi gonderilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Dogrulama e-postasini kuyruga birakir; gonderim hatasi yaniti bozmaz.
        /// </summary>
        /// <param name="user">Dogrulanacak kullanici.</param>
        /// <param name="rawToken">E-postaya yazilacak ham token.</param>
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
                // Kuyruk hatasi yeniden gonderim akisini bozmaz.
            }
        }
    }
}
