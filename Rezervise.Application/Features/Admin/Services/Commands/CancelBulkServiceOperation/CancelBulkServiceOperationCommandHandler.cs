using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation
{
    public class CancelBulkServiceOperationCommandHandler
        : IRequestHandler<CancelBulkServiceOperationCommand, Result<CancelledBulkOperationDto>>
    {
        private readonly IRepository<BulkOperation> _bulkOperationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public CancelBulkServiceOperationCommandHandler(
            IRepository<BulkOperation> bulkOperationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _bulkOperationRepository = bulkOperationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<CancelledBulkOperationDto>> Handle(
            CancelBulkServiceOperationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var operation = await _bulkOperationRepository.GetByIdAsync(
                    request.BulkOperationId, cancellationToken);

                if (operation is null)
                    return Result<CancelledBulkOperationDto>.Failure(
                        Error.NotFound("Toplu işlem", request.BulkOperationId.ToString()));

                // İşlem başlamışsa (Running) veya sonlanmışsa iptal edilmez.
                if (operation.Status != EnumBulkOperationStatus.Pending
                    && operation.Status != EnumBulkOperationStatus.Validating
                    && operation.Status != EnumBulkOperationStatus.AwaitingApproval)
                    return Result<CancelledBulkOperationDto>.Failure(
                        Error.Conflict("Toplu işlem bu durumda iptal edilemez.", $"Durum: {operation.Status}"));

                var previousStatus = operation.Status;
                operation.Cancel();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    operation.BusinessId,
                    entityType: "BulkOperation",
                    entityId: operation.Id.ToString(),
                    action: "Cancel",
                    oldValues: $"{{\"status\":\"{previousStatus}\"}}",
                    newValues: $"{{\"status\":\"{EnumBulkOperationStatus.Cancelled}\"}}",
                    cancellationToken: cancellationToken);

                var dto = new CancelledBulkOperationDto
                {
                    Id = operation.Id,
                    Status = operation.Status
                };

                return Result<CancelledBulkOperationDto>.Success(dto);
            }
            catch (DomainException ex)
            {
                return Result<CancelledBulkOperationDto>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<CancelledBulkOperationDto>.Failure(
                    Error.Internal("Toplu işlem iptal edilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
