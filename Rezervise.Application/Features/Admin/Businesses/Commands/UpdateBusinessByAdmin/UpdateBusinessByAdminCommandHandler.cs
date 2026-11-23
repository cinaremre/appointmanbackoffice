using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.UpdateBusinessByAdmin
{
    /// <summary>
    /// İşletme güncelleme handler'i (doküman Modül 8). Alan değişikliği
    /// entity'nin kendi Set metotlarıyla yapılır; kritik işlem öldüğü için
    /// denetim kaydına "Update" aksiyonuyla yazılır.
    /// </summary>
    public class UpdateBusinessByAdminCommandHandler : IRequestHandler<UpdateBusinessByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateBusinessByAdminCommandHandler(
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

        public async Task<Result<Guid>> Handle(UpdateBusinessByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. En az bir alan gönderilmeli.
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasDescription = request.Description is not null;

                if (!hasName && !hasDescription)
                    return Result<Guid>.Failure(
                        Error.Validation("Güncellenecek en az bir alan (Name veya Description) gereklidir."));

                // 2. Yönetici kimliği denetim kaydı için zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 4. Önceki değerler denetim kaydı için yakalanır.
                var oldValues = JsonSerializer.Serialize(new
                {
                    business.Name,
                    business.Description
                });

                // 5. Entity'nin kendi güncelleme metotları kullanılır.
                if (hasName)
                    business.SetName(request.Name!.Trim());

                if (hasDescription)
                    business.SetDescription(request.Description);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydı: actor JWT kimliğinden çözülür.
                await _auditSink.RecordAsync(
                    _currentUser.UserId.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "Update",
                    oldValues,
                    JsonSerializer.Serialize(new { business.Name, business.Description }),
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
                    Error.Internal("İşletme güncellenirken bir hata oluştu.", ex.Message));
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
