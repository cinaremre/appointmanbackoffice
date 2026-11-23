using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.CreateCommissionRule
{
    /// <summary>
    /// Yeni prim kuralı tanımlar. Kural bir çalışana, bir hizmete/ürüne veya
    /// işletme geneline uygulanabilir; kapsam alanları boş verilirse genel kuraldır.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class CreateCommissionRuleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        /// <summary>Boş ise kural işletme geneli/hizmet-ürün kuralıdır.</summary>
        public Guid? EmployeeId { get; set; }

        /// <summary>Boş ise kural hizmet-özel değildir.</summary>
        public Guid? ServiceId { get; set; }

        /// <summary>Boş ise kural ürün-özel değildir.</summary>
        public Guid? ProductId { get; set; }

        public EnumCommissionCalculationType CalculationType { get; set; }

        /// <summary>Yüzde tipleri için 0-1 arası oran (orn. %20 için 0.20).</summary>
        public decimal Rate { get; set; }

        /// <summary>FixedPerService tipi için hizmet basına sabit tutar.</summary>
        public decimal FixedAmount { get; set; }

        /// <summary>Kullanım penceresi başlangıcı (UTC).</summary>
        public DateTime EffectiveFrom { get; set; }

        /// <summary>Kullanım penceresi sonu (UTC, opsiyonel).</summary>
        public DateTime? EffectiveTo { get; set; }
    }
}
