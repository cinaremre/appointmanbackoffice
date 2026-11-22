using System.Text;
using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation
{
    public class ExecuteBulkServiceOperationCommandHandler
        : IRequestHandler<ExecuteBulkServiceOperationCommand, Result<ExecutedBulkOperationDto>>
    {
        private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Ilk surumde kayit basina senkron isleme chunk boyutu.
        /// Buyuk olcekte kuyruk tabanli islemciye tasinacaktir (dokuman 10.4).
        /// </summary>
        private const int ChunkSize = 200;

        private const int MaxErrorReportLength = 4000;

        private readonly IRepository<BulkOperation> _bulkOperationRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<ServiceCategory> _serviceCategoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public ExecuteBulkServiceOperationCommandHandler(
            IRepository<BulkOperation> bulkOperationRepository,
            IRepository<Service> serviceRepository,
            IRepository<ServiceCategory> serviceCategoryRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _bulkOperationRepository = bulkOperationRepository;
            _serviceRepository = serviceRepository;
            _serviceCategoryRepository = serviceCategoryRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<ExecutedBulkOperationDto>> Handle(
            ExecuteBulkServiceOperationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var operation = await _bulkOperationRepository.GetByIdAsync(
                    request.BulkOperationId, cancellationToken);

                if (operation is null)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.NotFound("Toplu islem", request.BulkOperationId.ToString()));

                // Onizleme kayitlari asla yurutulmez.
                if (operation.DryRun)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Onizleme (dry run) kayitlari yurutulemez; gercek islem icin yeni bir toplu islem olusturun."));

                // Yalnizca dogrulanmis/onay bekleyen isler baslatilabilir.
                // Tamamlanmis/iptal edilen/kosan isler Conflict doner.
                if (operation.Status != EnumBulkOperationStatus.Validating
                    && operation.Status != EnumBulkOperationStatus.AwaitingApproval)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu islem bu durumda yurutulemez.", $"Durum: {operation.Status}"));

                // Payload append-only: yurutme sirasinda yalnizca okunur.
                BulkServiceOperationPayload? payload;
                try
                {
                    payload = JsonSerializer.Deserialize<BulkServiceOperationPayload>(
                        operation.RequestedPayload, PayloadJsonOptions);
                }
                catch (JsonException)
                {
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu islem payload'i gecersiz; islem yurutulemez."));
                }

                if (payload is null || payload.ServiceIds.Count == 0)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu islem payload'i bos; islem yurutulemez."));

                // Kategori degisimi icin hedef kategori bir kez yuklenir.
                ServiceCategory? targetCategory = null;
                if (operation.OperationType == EnumBulkOperationType.ChangeServiceCategory)
                {
                    if (!payload.TargetCategoryId.HasValue)
                        return Result<ExecutedBulkOperationDto>.Failure(
                            Error.Conflict("Kategori degisimi icin hedef kategori payload'da yok."));

                    targetCategory = await _serviceCategoryRepository.GetByIdAsync(
                        payload.TargetCategoryId.Value, cancellationToken);

                    if (targetCategory is null)
                        return Result<ExecutedBulkOperationDto>.Failure(
                            Error.NotFound("Hizmet kategorisi", payload.TargetCategoryId.Value.ToString()));
                }

                // Durum gecisi: Validating/AwaitingApproval -> Running.
                operation.Start();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // PARCALI ISLEME: kayitlar chunk'lar halinde islenir. Ilk surumde
                // senkron yurutulur; asamali durum raporu SuccessCount/FailedCount
                // uzerinden takip edilir (dokuman 10.4).
                var successCount = 0;
                var failedCount = 0;
                var errorReport = new StringBuilder();
                var actorId = _currentUser.UserId?.ToString();

                foreach (var chunk in payload.ServiceIds.Chunk(ChunkSize))
                {
                    foreach (var serviceId in chunk)
                    {
                        try
                        {
                            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
                            if (service is null || service.IsDeleted)
                            {
                                failedCount++;
                                AppendError(errorReport, serviceId, "Hizmet bulunamadi veya silinmis.");
                                continue;
                            }

                            switch (operation.OperationType)
                            {
                                case EnumBulkOperationType.DeactivateServices:
                                    service.Deactivate(deactivatedBy: actorId);
                                    break;
                                case EnumBulkOperationType.ActivateServices:
                                    service.Activate(activatedBy: actorId);
                                    break;
                                case EnumBulkOperationType.ChangeServiceCategory:
                                    service.SetServiceCategory(targetCategory!);
                                    break;
                                default:
                                    failedCount++;
                                    AppendError(errorReport, serviceId,
                                        $"Desteklenmeyen islem tipi: {operation.OperationType}.");
                                    continue;
                            }

                            successCount++;
                        }
                        catch (DomainException ex)
                        {
                            // Tek kaydin kural hatasi tum islemi durdurmaz.
                            failedCount++;
                            AppendError(errorReport, serviceId, ex.Message);
                        }
                    }

                    // Chunk sonunda ara durum kaydedilir; uzun islemlerde
                    // guncel sayaclar veritabaninda gorunur kalir.
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var report = failedCount > 0 ? errorReport.ToString() : null;
                operation.Complete(successCount, failedCount);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    operation.BusinessId,
                    entityType: "BulkOperation",
                    entityId: operation.Id.ToString(),
                    action: "Execute",
                    oldValues: $"{{\"status\":\"{EnumBulkOperationStatus.Running}\"}}",
                    newValues: $"{{\"status\":\"{operation.Status}\",\"success\":{successCount},\"failed\":{failedCount}}}",
                    cancellationToken: cancellationToken);

                var dto = new ExecutedBulkOperationDto
                {
                    Id = operation.Id,
                    Status = operation.Status,
                    TotalRecords = operation.TotalRecords,
                    SuccessCount = operation.SuccessCount,
                    FailedCount = operation.FailedCount,
                    ErrorReport = report
                };

                return Result<ExecutedBulkOperationDto>.Success(dto);
            }
            catch (DomainException ex)
            {
                return Result<ExecutedBulkOperationDto>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<ExecutedBulkOperationDto>.Failure(
                    Error.Internal("Toplu islem yurutulurken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>Hata raporunu 4000 karakter sinirinda biriktirir.</summary>
        private static void AppendError(StringBuilder report, Guid serviceId, string reason)
        {
            if (report.Length >= MaxErrorReportLength)
                return;

            if (report.Length > 0)
                report.Append(" | ");

            report.Append($"{serviceId}: {reason}");
        }
    }
}
