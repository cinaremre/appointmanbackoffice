using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation
{
    /// <summary>
    /// Validating/AwaitingApproval durumundaki toplu işlemi yürütür
    /// (doküman Modül 10). İlk sürümde kayıtlar senkron ama chunk'lar
    /// halinde işlenir; büyük ölçekte kuyruk tabanlı işlemciye taşınacak.
    /// </summary>
    public class ExecuteBulkServiceOperationCommand : IRequest<Result<ExecutedBulkOperationDto>>
    {
        public Guid BulkOperationId { get; set; }
    }

    /// <summary>Yürütme sonucu: sayılar, son durum ve kırpılmış hata raporu.</summary>
    public class ExecutedBulkOperationDto
    {
        public Guid Id { get; set; }
        public EnumBulkOperationStatus Status { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string? ErrorReport { get; set; }
    }
}
