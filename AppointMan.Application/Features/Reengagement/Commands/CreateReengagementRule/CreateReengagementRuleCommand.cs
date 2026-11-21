using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Reengagement.Commands.CreateReengagementRule
{
    /// <summary>
    /// Isletme icin geri cagirma kurali olusturur (dokuman Modul 13).
    /// TemplateMessage degiskenleri: {{CustomerName}} {{BusinessName}}
    /// {{ServiceName}} {{LastVisitDate}} {{BookingUrl}}.
    /// </summary>
    public class CreateReengagementRuleCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Null = kural isletmedeki tum hizmetlere uygulanir.</summary>
        public Guid? ServiceId { get; set; }

        public string TemplateMessage { get; set; } = string.Empty;

        /// <summary>Kac gun randevusuz gecen musteri aday olur (> 0).</summary>
        public int IntervalDays { get; set; }

        /// <summary>IntervalDays'e kac gun onceden aday listesine girer (>= 0).</summary>
        public int LeadDays { get; set; }

        /// <summary>Bir dongrede gonderilebilecek azami mesaj (>= 1).</summary>
        public int MaxMessagesPerCycle { get; set; }

        /// <summary>Ayni musteriye tekrar mesaj icin beklenen gun (>= 0).</summary>
        public int CooldownDays { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}
