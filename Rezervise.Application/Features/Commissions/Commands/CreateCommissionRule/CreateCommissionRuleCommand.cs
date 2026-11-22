using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.CreateCommissionRule
{
    /// <summary>
    /// Yeni prim kurali tanimlar. Kural bir calisana, bir hizmete/urune veya
    /// isletme geneline uygulanabilir; kapsam alanlari bos verilirse genel kuraldir.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class CreateCommissionRuleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        /// <summary>Bos ise kural isletme geneli/hizmet-urun kuralidir.</summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>Bos ise kural hizmet-ozel degildir.</summary>
        public Guid? ServiceId { get; set; }

        /// <summary>Bos ise kural urun-ozel degildir.</summary>
        public Guid? ProductId { get; set; }

        public EnumCommissionCalculationType CalculationType { get; set; }

        /// <summary>Yuzde tipleri icin 0-1 arasi oran (orn. %20 icin 0.20).</summary>
        public decimal Rate { get; set; }

        /// <summary>FixedPerService tipi icin hizmet basina sabit tutar.</summary>
        public decimal FixedAmount { get; set; }

        /// <summary>Kullanim penceresi baslangici (UTC).</summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>Kullanim penceresi sonu (UTC, opsiyonel).</summary>
        public DateTime? EffectiveTo { get; set; }
    }
}
