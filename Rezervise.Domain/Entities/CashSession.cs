using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    /// <summary>
    /// Kasa oturumu. Gün başında açılır, gün sonunda sayılan nakit ile
    /// beklenen nakit karşılaştırılır ve fark rapolanır.
    /// Beklenen nakit açılış + nakit tahsilat + manuel giriş - manuel çıkış
    /// - nakit iade formülüyle hesaplanır; kart tahsilatlar dahil DEGILDIR.
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
                throw new ArgumentException("Kasa ID boş olamaz.", nameof(cashRegisterId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (openedBy == Guid.Empty)
                throw new ArgumentException("Oturumu açan kullanıcı ID boş olamaz.", nameof(openedBy));

            if (openingCash < 0)
                throw new DomainException("Açılış nakdi negatif olamaz.");

            CashRegisterId = cashRegisterId;
            BusinessId = businessId;
            OpenedBy = openedBy;
            OpeningCash = Math.Round(openingCash, 2);
            OpenedAt = now ?? DateTime.UtcNow;
            Status = EnumCashSessionStatus.Open;
        }

        /// <summary>
        /// Oturumu kapatır ve farkı rapolar. Nakit tahsilat, nakit iade ve
        /// manuel giriş/çıkış tutarları movement kayıtlarından handler
        /// tarafından hesaplanıp parametre olarak verilir.
        /// </summary>
        /// <param name="countedCash">Fiziksel olarak sayılan nakit.</param>
        /// <param name="totalCashCollected">Oturumdaki nakit tahsilat toplamı.</param>
        /// <param name="totalCashIn">Oturumdaki manuel nakit giriş toplamı.</param>
        /// <param name="totalCashOut">Oturumdaki manuel nakit çıkış toplamı.</param>
        /// <param name="totalCashRefunded">Oturumdaki nakit iade toplamı.</param>
        /// <param name="closedBy">Oturumu kapatan kullanıcı ID.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void Close(decimal countedCash, decimal totalCashCollected,
            decimal totalCashIn, decimal totalCashOut, decimal totalCashRefunded,
            Guid closedBy, DateTime? now = null)
        {
            if (Status == EnumCashSessionStatus.Closed)
                throw new DomainException("Kasa oturumu zaten kapalı.");

            if (closedBy == Guid.Empty)
                throw new ArgumentException("Oturumu kapatan kullanıcı ID boş olamaz.", nameof(closedBy));

            if (countedCash < 0)
                throw new DomainException("Sayılan nakit negatif olamaz.");

            if (totalCashCollected < 0 || totalCashIn < 0 || totalCashOut < 0 || totalCashRefunded < 0)
                throw new DomainException("Kasa bileşenleri negatif olamaz.");

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
