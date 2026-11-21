using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Services.Commands.CancelBulkServiceOperation
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
