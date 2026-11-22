using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.AccrueCommission
{
    /// <summary>
    /// Calisana prim tahakkuk eder. Tahakkuk TAHSILAT TABANLI varsayimla calisir:
    /// BaseAmount alani primin hesaplanacagi TAHSIL EDILEN tutar olarak verilir
    /// (dokumanin varsayimi); cagiran taraf tahsilati gerceklesmeden tahakkuk
    /// yapmamalidir. Uygun kural su an (UTC) gecerli pencerede olan ve kapsam
    /// alanlari eslesen kurallar arasindan en spesifik olanidir:
    /// EmployeeId+ServiceId eslesen > yalniz EmployeeId eslesen >
    /// yalniz ServiceId genel > kapsam alani olmayan genel kural.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class AccrueCommissionCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid EmployeeId { get; set; }

        /// <summary>Varsa bu tahakkugu doguran satis satiri.</summary>
        public Guid? SaleLineId { get; set; }

        /// <summary>Kural seciminde kullanilan hizmet ID (opsiyonel).</summary>
        public Guid? ServiceId { get; set; }

        /// <summary>Kural seciminde kullanilan urun ID (opsiyonel).</summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Primin hesaplanacagi tahsil edilen tutar. Tahsilat tabanli tahakkuk:
        /// bu deger cagiran taraf tarafindan tahsil edilen tutar olarak verilmelidir.
        /// </summary>
        public decimal BaseAmount { get; set; }
    }
}
