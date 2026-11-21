using System.Text.Json;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin
{
    /// <summary>
    /// Isletme aktif etme handler'i (dokuman Modul 8). Durum degisikligi
    /// Business.Activate metoduyla yapilir; "Activate" aksiyonu denetim
    /// kaydina onceki durum bilgisiyle yazilir.
    /// </summary>
    public class ActivateBusinessByAdminCommandHandler : IRequestHandler<ActivateBusinessByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public ActivateBusinessByAdminCommandHandler(
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(ActivateBusinessByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yonetici kimligi denetim kaydi icin zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 2. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 3. Zaten aktif olan isletme tekrar aktif edilmez.
                if (business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Isletme zaten aktif."));

                // 4. Onceki durum denetim kaydi icin yakalanir.
                var oldValues = JsonSerializer.Serialize(new { Status = "Suspended" });

                // 5. Entity'nin kendi durum degisim akisi kullanilir.
                business.Activate(_currentUser.UserId.Value.ToString());

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydi: Action=Activate, onceki durum bilgisiyle.
                await _auditSink.RecordAsync(
                    _currentUser.UserId.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "Activate",
                    oldValues,
                    JsonSerializer.Serialize(new { Status = "Active" }),
                    request.Reason,
                    _auditContext.CorrelationId,
                    _auditContext.IpAddress,
                    cancellationToken);

                return Result<Guid>.Success(business.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Isletme aktif edilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydina yazilacak rol: bilinen ilk rol; roller cozulemiyorsa
        /// geriye donuk uyumluluk icin "Admin" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "Admin";
    }
}
