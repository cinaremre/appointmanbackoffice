using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rezervise.Application.Common.Abstractions
{
    /// <summary>
    /// Rezervasyon öncesi ilgili çalışanın zaman aralığını veritabanı seviyesinde
    /// kilitler. SERIALIZABLE transaction içinde çağrılmalıdır; aksi halde
    /// aralık kilidi etkisiz kalır.
    /// Amaç: iki paralel isteğin aynı zaman aralığına randevu eklemesini engellemek.
    /// </summary>
    public interface IAppointmentBookingGuard
    {
        /// <summary>
        /// Verilen çalışan ve zaman aralığı için çakışan aktif randevu satırlarını
        /// kilitler ve çakışma olup olmadığını döndürür.
        /// </summary>
        /// <param name="employeeId">Randevu alınacak çalışan.</param>
        /// <param name="start">Aranılan aralığın başlangıcı.</param>
        /// <param name="end">Aranılan aralığın bitişi.</param>
        /// <param name="fallbackDurationMinutes">Bitiş saati boş ise kullanılacak süre (dakika).</param>
        /// <param name="excludeAppointmentId">Güncelleme sırasında kendiyle çakışmaması için dişlanan randevu.</param>
        /// <param name="cancellationToken">İptal tokeni.</param>
        /// <returns>Çakışan randevu varsa true.</returns>
        Task<bool> HasConflictAsync(
            Guid employeeId,
            DateTime start,
            DateTime end,
            int fallbackDurationMinutes,
            Guid? excludeAppointmentId = null,
            CancellationToken cancellationToken = default);
    }
}
