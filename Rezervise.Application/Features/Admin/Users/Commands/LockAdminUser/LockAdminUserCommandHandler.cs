using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Users.Commands.LockAdminUser
{
    /// <summary>
    /// Kullanıcı hesabını kilitler. Kilit işlemi domain'deki mevcut
    /// <see cref="User.LockAccount"/> metoduyla yapılır; yeni bir domain
    /// metodu eklenmez. Kritik işlem öldüğü için denetim kaydına yazılır
    /// (Action=LockUser, gerekçe zorunlu).
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
                // 1. Yönetici kimliği JWT'den çözülür; gövdedeki değerler geçersizdir.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "Unknown";

                // 2. Gerekçe zorunludur: kritik işlemler gerekçesiz yapılamaz.
                var reason = request.Reason?.Trim();
                if (string.IsNullOrEmpty(reason))
                    return Result.Failure(Error.Validation("Kilit gerekçesi zorunludur."));

                // 3. Kullanıcıyı yükle; yoksa NotFound.
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result.Failure(Error.NotFoundById("Kullanıcı", request.UserId));

                // 4. Domain kilidi: mevcut LockAccount metodu AccountStatus'u
                //    Locked yapar ve UserAccountLockedEvent üretir.
                var oldStatus = user.AccountStatus;
                user.LockAccount(reason, $"Admin:{actorId}");

                // 5. Kaydet.
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydı: correlation ve IP bağlamdan taşınır.
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
                    "Kullanıcı kilitleme sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
