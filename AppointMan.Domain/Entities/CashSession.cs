using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Kasa oturumu. Gun basinda acilir, gun sonunda sayilan nakit ile
    /// beklenen nakit karsilastirilir ve fark rapolanir.
    /// Beklenen nakit acilis + nakit tahsilat + manuel giris - manuel cikis
    /// - nakit iade formuluyle hesaplanir; kart tahsilatlar dahil DEGILDIR.
    /// </summary>
    public class CashSession : BaseEntity
    {
        public Guid CashRegisterId { get; private set; }
        public Guid BusinessId { get; private set; }
        public Guid OpenedBy { get; private set; }
        public DateTime OpenedAt { get; private set; }
        public decimal OpeningCash { get; private set; }

        public Guid? ClosedBy { get; private set; }
        public DateTime? ClosedAt { get; private set; }
        public decimal? ExpectedCash { get; private set; }
        public decimal? CountedCash { get; private set; }
        public decimal? Difference { get; private set; }

        public EnumCashSessionStatus Status { get; private set; }

        public CashRegister CashRegister { get; private set; }

        private CashSession() { }

        public CashSession(Guid cashRegisterId, Guid businessId, Guid openedBy,
            decimal openingCash, DateTime? now = null)
            : this(Guid.NewGuid(), cashRegisterId, businessId, openedBy, openingCash, now)
        {
        }

        public CashSession(Guid id, Guid cashRegisterId, Guid businessId, Guid openedBy,
            decimal openingCash, DateTime? now = null)
            : base(id)
        {
            if (cashRegisterId == Guid.Empty)
                throw new ArgumentException("Kasa ID bos olamaz.", nameof(cashRegisterId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (openedBy == Guid.Empty)
                throw new ArgumentException("Oturumu acan kullanici ID bos olamaz.", nameof(openedBy));

            if (openingCash < 0)
                throw new DomainException("Acilis nakdi negatif olamaz.");

            CashRegisterId = cashRegisterId;
            BusinessId = businessId;
            OpenedBy = openedBy;
            OpeningCash = Math.Round(openingCash, 2);
            OpenedAt = now ?? DateTime.UtcNow;
            Status = EnumCashSessionStatus.Open;
        }

        /// <summary>
        /// Oturumu kapatir ve farki rapolar. Nakit tahsilat, nakit iade ve
        /// manuel giris/cikis tutarlari movement kayitlarindan handler
        /// tarafindan hesaplanip parametre olarak verilir.
        /// </summary>
        /// <param name="countedCash">Fiziksel olarak sayilan nakit.</param>
        /// <param name="totalCashCollected">Oturumdaki nakit tahsilat toplami.</param>
        /// <param name="totalCashIn">Oturumdaki manuel nakit giris toplami.</param>
        /// <param name="totalCashOut">Oturumdaki manuel nakit cikis toplami.</param>
        /// <param name="totalCashRefunded">Oturumdaki nakit iade toplami.</param>
        /// <param name="closedBy">Oturumu kapatan kullanici ID.</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Close(decimal countedCash, decimal totalCashCollected,
            decimal totalCashIn, decimal totalCashOut, decimal totalCashRefunded,
            Guid closedBy, DateTime? now = null)
        {
            if (Status == EnumCashSessionStatus.Closed)
                throw new DomainException("Kasa oturumu zaten kapali.");

            if (closedBy == Guid.Empty)
                throw new ArgumentException("Oturumu kapatan kullanici ID bos olamaz.", nameof(closedBy));

            if (countedCash < 0)
                throw new DomainException("Sayilan nakit negatif olamaz.");

            if (totalCashCollected < 0 || totalCashIn < 0 || totalCashOut < 0 || totalCashRefunded < 0)
                throw new DomainException("Kasa bilesenleri negatif olamaz.");

            var currentTime = now ?? DateTime.UtcNow;

            ExpectedCash = Math.Round(
                OpeningCash + totalCashCollected + totalCashIn - totalCashOut - totalCashRefunded, 2);
            CountedCash = Math.Round(countedCash, 2);
            Difference = Math.Round(CountedCash.Value - ExpectedCash.Value, 2);
            Status = EnumCashSessionStatus.Closed;
            ClosedBy = closedBy;
            ClosedAt = currentTime;
            UpdatedAt = currentTime;
        }
    }
}
