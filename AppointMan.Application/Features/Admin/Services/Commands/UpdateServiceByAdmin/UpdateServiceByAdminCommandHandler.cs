using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Admin.Services.Commands.UpdateServiceByAdmin
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
                        Error.Validation("Guncellenecek en az bir alan gereklidir."));

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

                // Kritik islem: isletme sahibi degil platform admin'i degistirdi.
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
                    Error.Internal("Hizmet guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
