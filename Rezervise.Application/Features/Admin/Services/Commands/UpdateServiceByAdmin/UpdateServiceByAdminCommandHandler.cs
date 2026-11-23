using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Rezervise.Domain.ValueObjects;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin
{
    public class UpdateServiceByAdminCommandHandler
        : IRequestHandler<UpdateServiceByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Service> _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateServiceByAdminCommandHandler(
            IRepository<Service> serviceRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(
            UpdateServiceByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.Name is null && request.Price is null && request.DurationMinutes is null)
                    return Result<Guid>.Failure(
                        Error.Validation("Güncellenecek en az bir alan gereklidir."));

                var service = await _serviceRepository.GetByIdAsync(
                    request.ServiceId, cancellationToken);

                if (service is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Hizmet", request.ServiceId.ToString()));

                var oldValues = $"{{\"name\":\"{service.Name}\",\"price\":{service.Price?.Amount},\"durationMinutes\":{service.DurationInMinutes}}}";

                if (request.Name is not null)
                    service.SetName(request.Name.Trim());

                if (request.Price.HasValue)
                    service.UpdatePrice(new Money(request.Price.Value, request.Currency ?? "TRY"));

                if (request.DurationMinutes.HasValue)
                    service.SetDuration(request.DurationMinutes.Value);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newValues = $"{{\"name\":\"{service.Name}\",\"price\":{service.Price?.Amount},\"durationMinutes\":{service.DurationInMinutes}}}";

                // Kritik işlem: işletme sahibi değil platform admin'i değiştirdi.
                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    businessId: service.BusinessId,
                    entityType: "Service",
                    entityId: service.Id.ToString(),
                    action: "UpdateService",
                    oldValues,
                    newValues,
                    cancellationToken: cancellationToken);

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
    }
}
