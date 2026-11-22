using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation
{
    /// <summary>
    /// Pending/Validating/AwaitingApproval durumundaki toplu islemi iptal eder
    /// (dokuman Modul 10). Running ve sonrasi durumlar Conflict doner.
    /// </summary>
    public class CancelBulkServiceOperationCommand : IRequest<Result<CancelledBulkOperationDto>>
    {
        public Guid BulkOperationId { get; set; }
    }

    public class CancelledBulkOperationDto
    {
        public Guid Id { get; set; }
        public EnumBulkOperationStatus Status { get; set; }
    }
}
