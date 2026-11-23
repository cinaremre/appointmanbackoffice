using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.AccrueCommission
{
    /// <summary>
    /// Çalışana prim tahakkuk eder. Tahakkuk TAHSILAT TABANLI varsayımla çalışır:
    /// BaseAmount alani primin hesaplanacağı TAHSIL EDILEN tutar olarak verilir
    /// (dokümanın varsayımı); çağıran taraf tahsilatı gerçekleşmeden tahakkuk
    /// yapmamalıdır. Uygun kural su an (UTC) geçerli pencerede olan ve kapsam
    /// alanları eşleşen kurallar arasından en spesifik olanıdır:
    /// EmployeeId+ServiceId eşleşen > yalnız EmployeeId eşleşen >
    /// yalnız ServiceId genel > kapsam alani olmayan genel kural.
    /// OwnerId gövdeden değil JWT'den çözülür (Api katmanı set eder).
    /// </summary>
    public class AccrueCommissionCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid EmployeeId { get; set; }

        /// <summary>Varsa bu tahakkuğu doğuran satış satırı.</summary>
        public Guid? SaleLineId { get; set; }

        /// <summary>Kural seçiminde kullanılan hizmet ID (opsiyonel).</summary>
        public Guid? ServiceId { get; set; }

        /// <summary>Kural seçiminde kullanılan ürün ID (opsiyonel).</summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Primin hesaplanacağı tahsil edilen tutar. Tahsilat tabanlı tahakkuk:
        /// bu değer çağıran taraf tarafından tahsil edilen tutar olarak verilmelidir.
        /// </summary>
        public decimal BaseAmount { get; set; }
    }
}
