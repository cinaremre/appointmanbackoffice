using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.ApproveBusinessByAdmin
{
    /// <summary>
    /// Backoffice onayı (doküman Modül 8): onay bekleyen işletmeyi yayına alır.
    /// İşletme IsApproved=true olur ve sahibine "hoşgeldiniz" bildirimi gönderilir.
    /// </summary>
    public class ApproveBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
