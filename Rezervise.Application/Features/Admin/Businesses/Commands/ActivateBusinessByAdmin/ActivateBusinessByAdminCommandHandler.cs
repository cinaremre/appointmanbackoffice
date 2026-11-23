using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.ActivateBusinessByAdmin
{
    /// <summary>
    /// İşletme aktif etme handler'i (doküman Modül 8). Durum değişikliği
    /// Business.Activate metoduyla yapılır; "Activate" aksiyonu denetim
    /// kaydına önceki durum bilgisiyle yazılır.
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
                // 1. Yönetici kimliği denetim kaydı için zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 2. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Zaten aktif olan işletme tekrar aktif edilmez.
                if (business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("İşletme zaten aktif."));

                // 4. Önceki durum denetim kaydı için yakalanır.
                var oldValues = JsonSerializer.Serialize(new { Status = "Suspended" });

                // 5. Entity'nin kendi durum değişim akışı kullanılır.
                business.Activate(_currentUser.UserId.Value.ToString());

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydı: Action=Activate, önceki durum bilgisiyle.
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
                    Error.Internal("İşletme aktif edilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydına yazılacak rol: bilinen ilk rol; roller çözülemiyorsa
        /// geriye dönük uyumluluk için "Admin" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "Admin";
    }
}
