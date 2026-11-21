using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Owner.Services.Commands.UpdateMyService
{
    /// <summary>
    /// Isletme sahibinin hizmet guncellemesi. Alan degisikligi Service
    /// entity'nin SetName/UpdatePrice/SetDuration metotlariyla yapilir;
    /// basari sonrasi denetim kaydina "OwnerUpdateService" yazilir.
    /// </summary>
    public class UpdateMyServiceCommandHandler : IRequestHandler<UpdateMyServiceCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateMyServiceCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(
            UpdateMyServiceCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. En az bir alan gonderilmeli (PATCH semasi).
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasPrice = request.Price.HasValue;
                var hasDuration = request.DurationMinutes.HasValue;

                if (!hasName && !hasPrice && !hasDuration)
                    return Result<Guid>.Failure(
                        Error.Validation("Guncellenecek en az bir alan (Name, Price veya DurationMinutes) gereklidir."));

                // 2. Isletme sahipligi: owner kapsaminin tek kapisi.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                // 3. Hizmet kaydi ve isletme uyumlulugu: hizmet istenen
                //    isletmeye ait olmali; sahiplik zaten isletmeden
                //    gectigi icin bu kontrol yeterlidir.
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);

                if (service is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Hizmet", request.ServiceId));

                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Forbidden("Hizmet bu isletmeye ait degil."));

                // 4. Onceki degerler denetim kaydi icin yakalanir.
                var oldValues = $"{{\"name\":\"{service.Name}\",\"price\":{service.Price?.Amount},\"durationMinutes\":{service.DurationInMinutes}}}";

                // 5. Entity'nin kendi guncelleme metotlari kullanilir.
                if (hasName)
                    service.SetName(request.Name!.Trim());

                if (hasPrice)
                    service.UpdatePrice(new Money(
                        request.Price!.Value,
                        request.Currency ?? service.Price?.Currency ?? "TRY"));

                if (hasDuration)
                    service.SetDuration(request.DurationMinutes!.Value);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydi: actor JWT kimliginden cozulur.
                var newValues = $"{{\"name\":\"{service.Name}\",\"price\":{service.Price?.Amount},\"durationMinutes\":{service.DurationInMinutes}}}";

                await _auditSink.RecordAsync(
                    _currentUser.UserId!.Value,
                    ResolveActorRole(),
                    request.BusinessId,
                    "Service",
                    service.Id.ToString(),
                    "OwnerUpdateService",
                    oldValues,
                    newValues,
                    request.Reason,
                    _auditContext.CorrelationId,
                    _auditContext.IpAddress,
                    cancellationToken);

                return Result<Guid>.Success(service.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Hizmet guncellenirken bir hata olustu.", ex.Message));
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
