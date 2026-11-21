using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    /// <summary>
    /// Kasa hareketi (manuel giris/cikis, iade kaynakli cikis vb.).
    /// ReferenceId doluysa hareket bir odeme kaydindan turemistir
    /// (orn. nakit iadenin ters kaydi); bos ise manuel girilen harekettir.
    /// </summary>
    public class CashMovement : BaseEntity
    {
        public Guid BusinessId { get; private set; }
        public Guid CashSessionId { get; private set; }
        public EnumCashMovementType MovementType { get; private set; }
        public decimal Amount { get; private set; }
        public string? Reason { get; private set; }
        public Guid? ReferenceId { get; private set; }
        public Guid CreatedBy { get; private set; }

        private CashMovement() { }

        public CashMovement(Guid businessId, Guid cashSessionId,
            EnumCashMovementType movementType, decimal amount,
            string? reason, Guid? referenceId, Guid createdBy)
            : this(Guid.NewGuid(), businessId, cashSessionId, movementType,
                  amount, reason, referenceId, createdBy)
        {
        }

        public CashMovement(Guid id, Guid businessId, Guid cashSessionId,
            EnumCashMovementType movementType, decimal amount,
            string? reason, Guid? referenceId, Guid createdBy)
            : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (cashSessionId == Guid.Empty)
                throw new ArgumentException("Kasa oturumu ID bos olamaz.", nameof(cashSessionId));

            if (createdBy == Guid.Empty)
                throw new ArgumentException("Hareketi olusturan kullanici ID bos olamaz.", nameof(createdBy));

            if (amount <= 0)
                throw new DomainException("Kasa hareket tutari sifirdan buyuk olmalidir.");

            if (reason is not null && reason.Length > 500)
                throw new DomainException("Kasa hareket gerekcesi 500 karakterden uzun olamaz.");

            BusinessId = businessId;
            CashSessionId = cashSessionId;
            MovementType = movementType;
            Amount = Math.Round(amount, 2);
            Reason = reason;
            ReferenceId = referenceId;
            CreatedBy = createdBy;
        }
    }
}
