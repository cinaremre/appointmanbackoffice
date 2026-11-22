using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.LockAdminUser
{
    /// <summary>
    /// Kullanici hesabini kilitler. Kilit islemi domain'deki mevcut
    /// <see cref="User.LockAccount"/> metoduyla yapilir; yeni bir domain
    /// metodu eklenmez. Kritik islem oldugu icin denetim kaydina yazilir
    /// (Action=LockUser, gerekce zorunlu).
    /// </summary>
    public class LockAdminUserCommandHandler : IRequestHandler<LockAdminUserCommand, Result>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public LockAdminUserCommandHandler(
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

        public async Task<Result> Handle(LockAdminUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yonetici kimligi JWT'den cozulur; govdedeki degerler gecersizdir.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekce zorunludur: kritik islemler gerekcesiz yapilamaz.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result.Failure(Error.Validation("Kilit gerekcesi zorunludur."));

                // 3. Kullaniciyi yukle; yoksa NotFound.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result.Failure(Error.NotFoundById("Kullanici", request.UserId));

                // 4. Domain kilidi: mevcut LockAccount metodu AccountStatus'u
                //    Locked yapar ve UserAccountLockedEvent uretir.
                var oldStatus = user.AccountStatus;
                user.LockAccount(reason, $"Admin:{actorId}");

                // 5. Kaydet.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydi: correlation ve IP baglamdan tasinir.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    businessId: null,
                    entityType: "User",
                    entityId: user.Id.ToString(),
                    action: "LockUser",
                    oldValues: $"{{\"accountStatus\":\"{oldStatus}\"}}",
                    newValues: $"{{\"accountStatus\":\"{EnumAccountStatus.Locked}\"}}",
                    reason: reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Failure(Error.Internal(
                    "Kullanici kilitleme sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
