using System.Text.Json;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation
{
    public class PreviewBulkServiceOperationQueryHandler
        : IRequestHandler<PreviewBulkServiceOperationQuery, Result<BulkOperationDto>>
    {
        private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IRepository<BulkOperation> _bulkOperationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public PreviewBulkServiceOperationQueryHandler(
            IRepository<Service> serviceRepository,
            IRepository<ServiceCategory> serviceCategoryRepository,
            IRepository<BulkOperation> bulkOperationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _serviceRepository = serviceRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _bulkOperationRepository = bulkOperationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<BulkOperationDto>> Handle(
            PreviewBulkServiceOperationQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<BulkOperationDto>.Failure(Error.Unauthorized());

                var serviceIds = request.ServiceIds?.Distinct().ToList() ?? new List<Guid>();

                if (serviceIds.Count == 0)
                    return Result<BulkOperationDto>.Failure(
                        Error.Validation("Hizmet ID listesi bos olamaz."));

                // Ilk surum siniri: buyuk olcekte kuyruk tabanli islemciye tasinacak.
                if (serviceIds.Count > AdminPaging.MaxBulkRecords)
                    return Result<BulkOperationDto>.Failure(
                        Error.Conflict($"Ilk surumde en fazla {AdminPaging.MaxBulkRecords} kayit islenebilir."));

                // Kategori degisimi icin hedef kategori bastan dogrulanir.
                if (request.OperationType == EnumBulkOperationType.ChangeServiceCategory)
                {
                    if (!request.TargetCategoryId.HasValue)
                        return Result<BulkOperationDto>.Failure(
                            Error.Validation("Kategori degisimi icin hedef kategori zorunludur."));

                    var targetCategory = await _serviceCategoryRepository.GetByIdAsync(
                        request.TargetCategoryId.Value, cancellationToken);

                    if (targetCategory is null)
                        return Result<BulkOperationDto>.Failure(
                            Error.NotFound("Hizmet kategorisi", request.TargetCategoryId.Value.ToString()));
                }

                // Gecerli/gecersiz ID ayristirmasi: var olan ve silinmemis hizmetler.
                var validIds = new List<Guid>();
                var invalidIds = new List<Guid>();

                foreach (var serviceId in serviceIds)
                {
                    var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
                    if (service is not null && !service.IsDeleted)
                        validIds.Add(serviceId);
                    else
                        invalidIds.Add(serviceId);
                }

                // Payload append-only yazilir; sonrasinda kimse degistiremez.
                var payload = JsonSerializer.Serialize(
                    new BulkServiceOperationPayload
                    {
                        ServiceIds = serviceIds,
                        TargetCategoryId = request.TargetCategoryId
                    },
                    PayloadJsonOptions);

                var operation = new BulkOperation(
                    Guid.NewGuid(),
                    request.OperationType,
                    payload,
                    request.DryRun,
                    _currentUser.UserId.Value,
                    validIds.Count,
                    request.BusinessId);

                // Onizleme dogrulama asamasinda birakilir; execute Pending'i reddeder.
                operation.MarkValidating();

                await _bulkOperationRepository.AddAsync(operation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    request.BusinessId,
                    entityType: "BulkOperation",
                    entityId: operation.Id.ToString(),
                    action: "Preview",
                    newValues: $"{{\"operationType\":\"{request.OperationType}\",\"dryRun\":{request.DryRun},\"totalRecords\":{validIds.Count},\"invalidCount\":{invalidIds.Count}}}",
                    cancellationToken: cancellationToken);

                var dto = new BulkOperationDto
                {
                    Id = operation.Id,
                    OperationType = operation.OperationType,
                    DryRun = operation.DryRun,
                    Status = operation.Status,
                    TotalRecords = operation.TotalRecords,
                    InvalidIds = invalidIds
                };

                return Result<BulkOperationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BulkOperationDto>.Failure(
                    Error.Internal("Toplu islem onizlemesi olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
