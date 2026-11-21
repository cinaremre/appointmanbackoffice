using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>
    /// Belirli bir isletmenin belirli bir gunundeki (UTC) calisan cakismalarini
    /// listeler (dokuman Modul 12). Ayni calisana ayni randevu saatinde birden
    /// fazla aktif randevu varsa kayit doner. DuplicateKey index'i bu durumu
    /// zaten engeller; sorgu yalnizca tespit/raporlama amaclidir.
    /// </summary>
    public class GetAdminAppointmentConflictsQuery : IRequest<Result<IReadOnlyList<AdminAppointmentConflictDto>>>
    {
        /// <summary>Cakismanin arandigi isletme.</summary>
        public Guid BusinessId { get; set; }

        /// <summary>Cakismanin arandigi gun (UTC; saat bilgisi yok sayilir).</summary>
        public DateTime Date { get; set; }
    }
}
