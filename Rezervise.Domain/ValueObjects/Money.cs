using System;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; private set; }
        public string Currency { get; private set; }

        private Money() { } // EF Core için

        public Money(decimal amount, string currency = "TRY")
        {
            SetAmount(amount);
            SetCurrency(currency);
        }

        public void SetAmount(decimal amount)
        {
            if (amount < 0)
                throw new DomainException("Money.AmountNegative");

            if (amount > 99999999.99m)
                throw new DomainException("Money.AmountTooHigh");

            Amount = Math.Round(amount, 2);
        }

        public void SetCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new DomainException("Money.CurrencyRequired");

            if (currency.Length != 3)
                throw new DomainException("Money.CurrencyLength");

            Currency = currency.ToUpperInvariant();
        }

        public Money Add(Money other)
        {
            if (other.Currency != Currency)
                throw new DomainException("Money.CurrencyMismatchAdd");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (other.Currency != Currency)
                throw new DomainException("Money.CurrencyMismatchSubtract");

            if (Amount < other.Amount)
                throw new DomainException("Money.InsufficientBalance");

            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal multiplier)
        {
            if (multiplier < 0)
                throw new DomainException("Money.MultiplierNegative");

            return new Money(Amount * multiplier, Currency);
        }

        public Money Divide(decimal divisor)
        {
            if (divisor <= 0)
                throw new DomainException("Money.DivisorPositive");

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
                throw new DomainException("Money.CurrencyMismatchCompare");

            return left.Amount > right.Amount;
        }

        public static bool operator <(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Money.CurrencyMismatchCompare");

            return left.Amount < right.Amount;
        }

        public static bool operator >=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Money.CurrencyMismatchCompare");

            return left.Amount >= right.Amount;
        }

        public static bool operator <=(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new DomainException("Money.CurrencyMismatchCompare");

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