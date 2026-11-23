using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin
{
    /// <summary>
    /// İşletme askıya alma handler'i (doküman Modül 8). Durum değişikliği
    /// Business.Suspend metoduyla yapılır; "Suspend" aksiyonu denetim
    /// kaydına önceki durum bilgisiyle yazılır.
    /// </summary>
    public class SuspendBusinessByAdminCommandHandler : IRequestHandler<SuspendBusinessByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public SuspendBusinessByAdminCommandHandler(
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

        public async Task<Result<Guid>> Handle(SuspendBusinessByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Gerekçe zorunludur; denetim kaydını anlamsız kılmamak için
                //    boş gönderim doğrulama hatasıyla reddedilir.
                if (string.IsNullOrWhiteSpace(request.Reason))
                    return Result<Guid>.Failure(
                        Error.Validation("Askıda alma gerekçesi (Reason) zorunludur."));

                // 2. Yönetici kimliği denetim kaydı için zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 4. Zaten askıda olan işletme tekrar askıya alınamaz.
                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("İşletme zaten askıda."));

                // 5. Önceki durum denetim kaydı için yakalanır.
                var oldValues = JsonSerializer.Serialize(new { Status = "Active" });

                // 6. Entity'nin kendi durum değişim akışı kullanılır.
                business.Suspend(_currentUser.UserId.Value.ToString(), request.Reason);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 7. Denetim kaydı: Action=Suspend, önceki durum bilgisiyle.
                await _auditSink.RecordAsync(
                    _currentUser.UserId.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "Suspend",
                    oldValues,
                    JsonSerializer.Serialize(new { Status = "Suspended" }),
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
                    Error.Internal("İşletme askıya alınırken bir hata oluştu.", ex.Message));
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
