using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Commands.ExecuteBulkServiceOperation
{
    /// <summary>
    /// Validating/AwaitingApproval durumundaki toplu islemi yurutur
    /// (dokuman Modul 10). Ilk surumde kayitlar senkron ama chunk'lar
    /// halinde islenir; buyuk olcekte kuyruk tabanli islemciye tasinacak.
    /// </summary>
    public class ExecuteBulkServiceOperationCommand : IRequest<Result<ExecutedBulkOperationDto>>
    {
        public Guid BulkOperationId { get; set; }
    }

    /// <summary>Yurutme sonucu: sayilar, son durum ve kirpilmis hata raporu.</summary>
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
