using System.Text.Json;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness
{
    /// <summary>
    /// Isletme sahibinin kendi isletme bilgilerini gunceller. Alan
    /// degisikligi Business entity'nin Set metotlariyla yapilir; basari
    /// sonrasi denetim kaydina "OwnerUpdateBusiness" aksiyonu yazilir.
    /// </summary>
    public class UpdateMyBusinessCommandHandler : IRequestHandler<UpdateMyBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateMyBusinessCommandHandler(
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

        public async Task<Result<Guid>> Handle(
            UpdateMyBusinessCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. En az bir alan gonderilmeli (PATCH semasi).
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasDescription = request.Description is not null;

                if (!hasName && !hasDescription)
                    return Result<Guid>.Failure(
                        Error.Validation("Guncellenecek en az bir alan (Name veya Description) gereklidir."));

                // 2. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                var business = owned.Value!;

                // 3. Onceki degerler denetim kaydi icin yakalanir.
                var oldValues = JsonSerializer.Serialize(new
                {
                    business.Name,
                    business.Description
                });

                // 4. Entity'nin kendi guncelleme metotlari kullanilir.
                if (hasName)
                    business.SetName(request.Name!.Trim());

                if (hasDescription)
                    business.SetDescription(request.Description);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Denetim kaydi: actor JWT kimliginden cozulur.
                await _auditSink.RecordAsync(
                    _currentUser.UserId!.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "OwnerUpdateBusiness",
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
        /// geriye donuk uyumluluk icin "BusinessOwner" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";
    }
}
