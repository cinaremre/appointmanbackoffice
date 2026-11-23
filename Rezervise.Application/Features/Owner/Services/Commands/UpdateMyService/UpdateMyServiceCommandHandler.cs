using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Owner.Services.Commands.UpdateMyService
{
    /// <summary>
    /// İşletme sahibinin hizmet güncellemesi. Alan değişikliği Service
    /// entity'nin SetName/UpdatePrice/SetDuration metotlarıyla yapılır;
    /// başarı sonrası denetim kaydına "OwnerUpdateService" yazılır.
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
                // 1. En az bir alan gönderilmeli (PATCH şeması).
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasPrice = request.Price.HasValue;
                var hasDuration = request.DurationMinutes.HasValue;

                if (!hasName && !hasPrice && !hasDuration)
                    return Result<Guid>.Failure(
                        Error.Validation("Güncellenecek en az bir alan (Name, Price veya DurationMinutes) gereklidir."));

                // 2. İşletme sahipliği: owner kapsamının tek kapısı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                // 3. Hizmet kaydı ve işletme uyumluluğu: hizmet istenen
                //    işletmeye ait olmalı; sahiplik zaten işletmeden
                //    geçtiği için bu kontrol yeterlidir.
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);

                if (service is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Hizmet", request.ServiceId));

                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Forbidden("Hizmet bu işletmeye ait değil."));

                // 4. Önceki değerler denetim kaydı için yakalanır.
                var oldValues = $"{{\"name\":\"{service.Name}\",\"price\":{service.Price?.Amount},\"durationMinutes\":{service.DurationInMinutes}}}";

                // 5. Entity'nin kendi güncelleme metotları kullanılır.
                if (hasName)
                    service.SetName(request.Name!.Trim());

                if (hasPrice)
                    service.UpdatePrice(new Money(
                        request.Price!.Value,
                        request.Currency ?? service.Price?.Currency ?? "TRY"));

                if (hasDuration)
                    service.SetDuration(request.DurationMinutes!.Value);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Denetim kaydı: actor JWT kimliğinden çözülür.
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
                    Error.Internal("Hizmet güncellenirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydına yazılacak rol: bilinen ilk rol; roller çözülemiyorsa
        /// geriye dönük uyumluluk için "BusinessOwner" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";
    }
}
