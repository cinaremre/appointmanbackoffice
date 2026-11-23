using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Reengagement.Commands.CreateReengagementRule
{
    /// <summary>
    /// İşletme için geri çağırma kuralı oluşturur (doküman Modül 13).
    /// TemplateMessage değişkenleri: {{CustomerName}} {{BusinessName}}
    /// {{ServiceName}} {{LastVisitDate}} {{BookingUrl}}.
    /// </summary>
    public class CreateReengagementRuleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Null = kural işletmedeki tüm hizmetlere uygulanır.</summary>
        public Guid? ServiceId { get; set; }

        public string TemplateMessage { get; set; } = string.Empty;

        /// <summary>Kaç gün randevusuz geçen müşteri aday olur (> 0).</summary>
        public int IntervalDays { get; set; }

        /// <summary>IntervalDays'e kaç gün önceden aday listesine girer (>= 0).</summary>
        public int LeadDays { get; set; }

        /// <summary>Bir döngrede gönderilebilecek azami mesaj (>= 1).</summary>
        public int MaxMessagesPerCycle { get; set; }

        /// <summary>Aynı müşteriye tekrar mesaj için beklenen gün (>= 0).</summary>
        public int CooldownDays { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}
