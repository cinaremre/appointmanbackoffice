using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Users.Commands.UnlockAdminUser
{
    /// <summary>
    /// Kullanici hesabinin kilidini acar. Kilit acma domain'deki mevcut
    /// <see cref="User.UnlockAccount"/> metoduyla yapilir. Kritik islem
    /// oldugu icin denetim kaydina yazilir (Action=UnlockUser).
    /// </summary>
    public class UnlockAdminUserCommandHandler : IRequestHandler<UnlockAdminUserCommand, Result>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UnlockAdminUserCommandHandler(
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result> Handle(UnlockAdminUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yonetici kimligi JWT'den cozulur.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekce zorunludur.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result.Failure(Error.Validation("Kilit acma gerekcesi zorunludur."));

                // 3. Kullaniciyi yukle; yoksa NotFound.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result.Failure(Error.NotFoundById("Kullanici", request.UserId));

                // 4. Domain kilidi acma: mevcut UnlockAccount metodu AccountStatus'u
                //    Active yapar ve UserAccountUnlockedEvent uretir.
                var oldStatus = user.AccountStatus;
                user.UnlockAccount($"Admin:{actorId}");

                // 5. Kaydet.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydi.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    businessId: null,
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    action: "UnlockUser",
                    oldValues: $"{{\"accountStatus\":\"{oldStatus}\"}}",
                    newValues: $"{{\"accountStatus\":\"{EnumAccountStatus.Active}\"}}",
                    reason: reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(Error.Internal(
                    "Kullanici kilidi acilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
