using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.DefineServiceMaterial
{
    /// <summary>
    /// Hizmet reçetesi tanımlar: bir hizmet gerçekleştirildiğinde
    /// tüketilecek malzemeyi ve miktarı belirtir. Aynı (ServiceId, ProductId)
    /// çifti işletme bazında bir kez tanımlanabilir.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class DefineServiceMaterialCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid ServiceId { get; set; }

        public Guid ProductId { get; set; }

        /// <summary>Bir hizmet gerçekleştirildiğinde tüketilen miktar (pozitif).</summary>
        public decimal QuantityPerService { get; set; }

        /// <summary>Ölçü birimi (varsayılan "adet").</summary>
        public string Unit { get; set; } = "adet";
    }
}
