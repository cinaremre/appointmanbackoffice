using System;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        private Money() { } // EF Core icin

        public Money(decimal amount, string currency = "TRY")
        {
            SetAmount(amount);
            SetCurrency(currency);
        }

        public void SetAmount(decimal amount)
        {
            if (amount < 0)
                throw new DomainException("Tutar negatif olamaz.");

            if (amount > 99999999.99m)
                throw new DomainException("Tutar cok yuksek.");

            Amount = Math.Round(amount, 2);
        }

        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Para birimi bos olamaz.");

            if (currency.Length != 3)
                throw new DomainException("Para birimi 3 karakter olmalidir.");

            Currency = currency.ToUpperInvariant();
        }

        public Money Add(Money other)
        {
            if (other.Currency != Currency)
                throw new DomainException("Farkli para birimleri toplanamaz.");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (other.Currency != Currency)
                throw new DomainException("Farkli para birimleri cikarilamaz.");

            if (Amount < other.Amount)
                throw new DomainException("Yetersiz bakiye.");

            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal multiplier)
        {
            if (multiplier < 0)
                throw new DomainException("Carpan negatif olamaz.");

            return new Money(Amount * multiplier, Currency);
        }

        public Money Divide(decimal divisor)
        {
            if (divisor <= 0)
                throw new DomainException("Bolen 0'dan buyuk olmalidir.");

            return new Money(Amount / divisor, Currency);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Money);
        }

        public bool Equals(Money? other)
        {
            if (other is null)
                return false;

            return Amount == other.Amount && Currency == other.Currency;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Amount, Currency);
        }

        public override string ToString()
        {
            return $"{Amount:F2} {Currency}";
        }

        public static bool operator >(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Farkli para birimleri karsilastirilamaz.");

            return left.Amount > right.Amount;
        }

        public static bool operator <(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Farkli para birimleri karsilastirilamaz.");

            return left.Amount < right.Amount;
        }

        public static bool operator >=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Farkli para birimleri karsilastirilamaz.");

            return left.Amount >= right.Amount;
        }

        public static bool operator <=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Farkli para birimleri karsilastirilamaz.");

            return left.Amount <= right.Amount;
        }

        public static bool operator ==(Money left, Money right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !(left == right);
        }

        public static implicit operator decimal(Money money)
        {
            return money?.Amount ?? 0;
        }

        public static implicit operator Money(decimal amount)
        {
            return new Money(amount);
        }
    }
}