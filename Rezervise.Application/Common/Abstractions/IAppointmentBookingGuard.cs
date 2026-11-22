using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rezervise.Application.Common.Abstractions
{
    /// <summary>
    /// Rezervasyon oncesi ilgili calisanin zaman araligini veritabani seviyesinde
    /// kilitler. SERIALIZABLE transaction icinde cagrilmalidir; aksi halde
    /// aralik kilidi etkisiz kalir.
    /// Amac: iki paralel istegin ayni zaman araligina randevu eklemesini engellemek.
    /// </summary>
    public interface IAppointmentBookingGuard
    {
        /// <summary>
        /// Verilen calisan ve zaman araligi icin cakisan aktif randevu satirlarini
        /// kilitler ve cakisma olup olmadigini dondurur.
        /// </summary>
        /// <param name="employeeId">Randevu alinacak calisan.</param>
        /// <param name="start">Aranilan araligin baslangici.</param>
        /// <param name="end">Aranilan araligin bitisi.</param>
        /// <param name="fallbackDurationMinutes">Bitis saati bos ise kullanilacak sure (dakika).</param>
        /// <param name="excludeAppointmentId">Guncelleme sirasinda kendiyle cakismamasi icin dislanan randevu.</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        /// <returns>Cakisan randevu varsa true.</returns>
        Task<bool> HasConflictAsync(
            Guid employeeId,
            DateTime start,
            DateTime end,
            int fallbackDurationMinutes,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default);
    }
}
