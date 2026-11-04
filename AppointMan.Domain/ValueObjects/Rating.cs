using System;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.ValueObjects
{
    public class Rating : IEquatable<Rating>
    {
        public int Value { get; private set; }

        private Rating() { } // EF Core icin

        public Rating(int value)
        {
            if (value < 1 || value > 5)
                throw new DomainException("Puan 1-5 arasinda olmalidir.");

            Value = value;
        }

        public static Rating FromDecimal(decimal value)
        {
            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 1) rounded = 1;
            if (rounded > 5) rounded = 5;
            return new Rating(rounded);
        }

        public static Rating FromDouble(double value)
        {
            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded < 1) rounded = 1;
            if (rounded > 5) rounded = 5;
            return new Rating(rounded);
        }

        public decimal ToDecimal()
        {
            return Value;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Rating);
        }

        public bool Equals(Rating? other)
        {
            if (other is null)
                return false;

            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator int(Rating rating)
        {
            return rating?.Value ?? 0;
        }

        public static implicit operator Rating(int value)
        {
            return new Rating(value);
        }

        public static implicit operator decimal(Rating rating)
        {
            return rating?.Value ?? 0;
        }

        public static implicit operator Rating(decimal value)
        {
            return FromDecimal(value);
        }
    }
}