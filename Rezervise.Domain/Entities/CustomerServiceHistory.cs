using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Musteri x hizmet bazinda ziyaret ozeti (dokuman Modul 13).
    /// Geri cagirma aday hesabi bu kayitlar uzerinden yapilir.
    /// </summary>
    public class CustomerServiceHistory : BaseEntity
    {
        public Guid BusinessId { get; private set; }

        /// <summary>Musterinin User ID'si.</summary>
        public Guid CustomerId { get; private set; }

        public Guid ServiceId { get; private set; }

        /// <summary>Son tamamlanan randevunun UTC ani.</summary>
        public DateTime LastCompletedAt { get; private set; }

        public int VisitCount { get; private set; }

        /// <summary>
        /// Tamamlanan ziyaretler arasindaki medyan gun araligi.
        /// VisitCount &lt; 2 ise hesaplanamaz; null kalir.
        /// </summary>
        public int? MedianIntervalDays { get; private set; }

        // Navigation Properties
        public Business Business { get; private set; }
        public User Customer { get; private set; }
        public Service Service { get; private set; }

        private CustomerServiceHistory() { } // EF Core

        public CustomerServiceHistory(Guid businessId, Guid customerId, Guid serviceId)
            : base(Guid.NewGuid())
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (customerId == Guid.Empty)
                throw new ArgumentException("Musteri ID bos olamaz.", nameof(customerId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            BusinessId = businessId;
            CustomerId = customerId;
            ServiceId = serviceId;
        }

        /// <summary>
        /// Tamamlanan randevu tarihlerinden istatistikleri yeniden hesaplar.
        /// Medyan aralik: sirali tarihlere komsu farklarin medyani;
        /// ziyaret sayisi 2'den azsa medyan null'dir.
        /// </summary>
        /// <param name="completedDates">Tamamlanan randevularin tarihleri (UTC).</param>
        /// <param name="now">Referans UTC an; bos liste durumunda kullanilmaz.</param>
        public void RecalculateFrom(List<DateTime> completedDates, DateTime now)
        {
            if (completedDates is null)
                throw new ArgumentNullException(nameof(completedDates));

            var dates = completedDates
                .Select(d => DateTime.SpecifyKind(d, DateTimeKind.Utc))
                .OrderBy(d => d)
                .ToList();

            VisitCount = dates.Count;

            if (dates.Count == 0)
            {
                // Hic ziyaret yoksa gecmis bir an isaretle; aday listesinde
                // IntervalDays kontrolu onu dogal olarak eler.
                LastCompletedAt = DateTime.SpecifyKind(now, DateTimeKind.Utc);
                MedianIntervalDays = null;
                UpdatedAt = DateTime.UtcNow;
                return;
            }

            LastCompletedAt = dates[dates.Count - 1];

            if (dates.Count < 2)
            {
                MedianIntervalDays = null;
                UpdatedAt = DateTime.UtcNow;
                return;
            }

            var intervals = new List<int>();

            for (var i = 1; i < dates.Count; i++)
            {
                var days = (dates[i].Date - dates[i - 1].Date).TotalDays;
                intervals.Add((int)days);
            }

            intervals.Sort();

            MedianIntervalDays = intervals.Count % 2 == 1
                ? intervals[intervals.Count / 2]
                : (int)Math.Round(
                    (intervals[intervals.Count / 2 - 1] + intervals[intervals.Count / 2]) / 2.0,
                    MidpointRounding.AwayFromZero);

            UpdatedAt = DateTime.UtcNow;
        }
    }
}
