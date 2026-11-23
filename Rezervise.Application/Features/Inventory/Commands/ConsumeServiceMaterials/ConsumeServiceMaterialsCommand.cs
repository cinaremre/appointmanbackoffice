using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.ConsumeServiceMaterials
{
    /// <summary>
    /// Tamamlanmış bir randevunun gerçek malzeme tüketimini stok hareketlerine
    /// işler. Miktarlar varsayılan reçeteden otomatik önerilmez; çağrı tarafından
    /// gerçekleşen tüketim olarak verilir. Hareketler ServiceConsumption tipinde,
    /// ReferenceType="Appointment" ve ReferenceId=randevu ID olarak yazılır;
    /// böylece aynı randevu için tüketim ikinci kez işlenemez.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class ConsumeServiceMaterialsCommand : IRequest<Result<int>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid AppointmentId { get; set; }

        /// <summary>Gerçekleşen tüketim listesi: (ürün, miktar) çiftleri.</summary>
        public List<ConsumeMaterialLine> Materials { get; set; } = new();
    }

    /// <summary>Tüketilen tek malzeme satırı.</summary>
    public class ConsumeMaterialLine
    {
        public Guid ProductId { get; set; }

        /// <summary>Tüketilen miktar (pozitif).</summary>
        public decimal Quantity { get; set; }
    }
}
