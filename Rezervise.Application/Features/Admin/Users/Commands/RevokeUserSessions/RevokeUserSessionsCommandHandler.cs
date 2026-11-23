using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.RevokeUserSessions
{
    /// <summary>
    /// Kullanıcının tüm oturumlarını geçersiz kılar. User entity'sindeki mevcut
    /// <see cref="User.RevokeRefreshToken"/> metodu aktif ve önceki refresh
    /// token hash'lerini null'a çeker; yeni bir domain metodu eklenmez.
    /// Kritik işlem öldüğü için denetim kaydına yazılır (Action=RevokeSessions).
    /// </summary>
    public class RevokeUserSessionsCommandHandler : IRequestHandler<RevokeUserSessionsCommand, Result>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTimeService;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public RevokeUserSessionsCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTimeService,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeService = dateTimeService;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(RevokeUserSessionsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yönetici kimliği JWT'den çözülür.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekçe zorunludur.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result.Failure(Error.Validation("Oturum iptali gerekçesi zorunludur."));

                // 3. Kullanıcıyı yükle; yoksa NotFound.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result.Failure(Error.NotFoundById("Kullanıcı", request.UserId));

                // 4. Oturumları geçersiz kil: mevcut RevokeRefreshToken metodu
                //    aktif ve önceki hash alanlarını temizler. Böylece kullanıcı
                //    elindeki refresh token ile yeni access token alamaz.
                var hadActiveToken = user.RefreshTokenHash is not null;
                user.RevokeRefreshToken(_dateTimeService.UtcNow);

                // 5. Kaydet.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydı: hash değerleri asla kayda yazılmaz.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    businessId: null,
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    action: "RevokeSessions",
                    oldValues: $"{{\"hasActiveRefreshToken\":{hadActiveToken.ToString().ToLowerInvariant()}}}",
                    newValues: $"{{\"hasActiveRefreshToken\":false,\"revokedAtUtc\":\"{_dateTimeService.UtcNow:O}\"}}",
                    reason: reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(Error.Internal(
                    "Kullanıcı oturumları iptal edilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
