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
    /// Isletme guncelleme handler'i (dokuman Modul 8). Alan degisikligi
    /// entity'nin kendi Set metotlariyla yapilir; kritik islem oldugu icin
    /// denetim kaydina "Update" aksiyonuyla yazilir.
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
                // 1. En az bir alan gonderilmeli.
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasDescription = request.Description is not null;

                if (!hasName && !hasDescription)
                    return Result<Guid>.Failure(
                        Error.Validation("Guncellenecek en az bir alan (Name veya Description) gereklidir."));

                // 2. Yonetici kimligi denetim kaydi icin zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 4. Onceki degerler denetim kaydi icin yakalanir.
                var oldValues = JsonSerializer.Serialize(new
                {
                    business.Name,
                    business.Description
                });

                // 5. Entity'nin kendi guncelleme metotlari kullanilir.
                if (hasName)
                    business.SetName(request.Name!.Trim());

                if (hasDescription)
                    business.SetDescription(request.Description);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydi: actor JWT kimliginden cozulur.
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
                    Error.Internal("Isletme guncellenirken bir hata olustu.", ex.Message));
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
