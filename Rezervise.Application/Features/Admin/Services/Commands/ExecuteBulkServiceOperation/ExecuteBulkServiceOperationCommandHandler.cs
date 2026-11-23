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
        /// İlk sürümde kayıt basına senkron işleme chunk boyutu.
        /// Büyük ölçekte kuyruk tabanlı işlemciye taşınacaktır (doküman 10.4).
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
                        Error.NotFound("Toplu işlem", request.BulkOperationId.ToString()));

                // Onizleme kayıtları asla yürütülmez.
                if (operation.DryRun)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Onizleme (dry run) kayıtları yürütülemez; gerçek işlem için yeni bir toplu işlem oluşturun."));

                // Yalnızca doğrulanmış/onay bekleyen işler başlatılabilir.
                // Tamamlanmış/iptal edilen/koşan işler Conflict döner.
                if (operation.Status != EnumBulkOperationStatus.Validating
                    && operation.Status != EnumBulkOperationStatus.AwaitingApproval)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu işlem bu durumda yürütülemez.", $"Durum: {operation.Status}"));

                // Payload append-only: yürütme sırasında yalnızca okunur.
                BulkServiceOperationPayload? payload;
                try
                {
                    payload = JsonSerializer.Deserialize<BulkServiceOperationPayload>(
                        operation.RequestedPayload, PayloadJsonOptions);
                }
                catch (JsonException)
                {
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu işlem payload'i geçersiz; işlem yürütülemez."));
                }

                if (payload is null || payload.ServiceIds.Count == 0)
                    return Result<ExecutedBulkOperationDto>.Failure(
                        Error.Conflict("Toplu işlem payload'i boş; işlem yürütülemez."));

                // Kategori değişimi için hedef kategori bir kez yüklenir.
                ServiceCategory? targetCategory = null;
                if (operation.OperationType == EnumBulkOperationType.ChangeServiceCategory)
                {
                    if (!payload.TargetCategoryId.HasValue)
                        return Result<ExecutedBulkOperationDto>.Failure(
                            Error.Conflict("Kategori değişimi için hedef kategori payload'da yok."));

                    targetCategory = await _serviceCategoryRepository.GetByIdAsync(
                        payload.TargetCategoryId.Value, cancellationToken);

                    if (targetCategory is null)
                        return Result<ExecutedBulkOperationDto>.Failure(
                            Error.NotFound("Hizmet kategorisi", payload.TargetCategoryId.Value.ToString()));
                }

                // Durum geçişi: Validating/AwaitingApproval -> Running.
                operation.Start();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // PARCALI ISLEME: kayıtlar chunk'lar halinde işlenir. İlk sürümde
                // senkron yürütülür; aşamalı durum raporu SuccessCount/FailedCount
                // üzerinden takip edilir (doküman 10.4).
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
                                AppendError(errorReport, serviceId, "Hizmet bulunamadı veya silinmiş.");
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
                                        $"Desteklenmeyen işlem tipi: {operation.OperationType}.");
                                    continue;
                            }

                            successCount++;
                        }
                        catch (DomainException ex)
                        {
                            // Tek kaydın kural hatası tüm işlemi durdurmaz.
                            failedCount++;
                            AppendError(errorReport, serviceId, ex.Message);
                        }
                    }

                    // Chunk sonunda ara durum kaydedilir; uzun işlemlerde
                    // güncel sayaçlar veritabanında görünür kalır.
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
                    Error.Internal("Toplu işlem yürütülürken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>Hata raporunu 4000 karakter sınırında biriktirir.</summary>
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
