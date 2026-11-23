using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>
    /// Belirli bir işletmenin belirli bir günündeki (UTC) çalışan çakışmalarını
    /// listeler (doküman Modül 12). Aynı çalışana aynı randevu saatinde birden
    /// fazla aktif randevu varsa kayıt döner. DuplicateKey index'i bu durumu
    /// zaten engeller; sorgu yalnızca tespit/raporlama amaçlıdır.
    /// </summary>
    public class GetAdminAppointmentConflictsQuery : IRequest<Result<IReadOnlyList<AdminAppointmentConflictDto>>>
    {
        /// <summary>Çakışmanın arandığı işletme.</summary>
        public Guid BusinessId { get; set; }

        /// <summary>Çakışmanın arandığı gün (UTC; saat bilgisi yok sayılır).</summary>
        public DateTime Date { get; set; }
    }
}
