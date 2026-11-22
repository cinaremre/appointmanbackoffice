using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.ConsumeServiceMaterials
{
    /// <summary>
    /// Tamamlanmis bir randevunun gercek malzeme tuketimini stok hareketlerine
    /// isler. Miktarlar varsayilan receteden otomatik onerilmez; cagri tarafindan
    /// gerceklesen tuketim olarak verilir. Hareketler ServiceConsumption tipinde,
    /// ReferenceType="Appointment" ve ReferenceId=randevu ID olarak yazilir;
    /// boylece ayni randevu icin tuketim ikinci kez islenemez.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class ConsumeServiceMaterialsCommand : IRequest<Result<int>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid AppointmentId { get; set; }

        /// <summary>Gerceklesen tuketim listesi: (urun, miktar) ciftleri.</summary>
        public List<ConsumeMaterialLine> Materials { get; set; } = new();
    }

    /// <summary>Tuketilen tek malzeme satiri.</summary>
    public class ConsumeMaterialLine
    {
        public Guid ProductId { get; set; }

        /// <summary>Tuketilen miktar (pozitif).</summary>
        public decimal Quantity { get; set; }
    }
}
