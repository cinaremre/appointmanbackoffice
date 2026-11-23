using System;
using System.Text.RegularExpressions;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.ValueObjects
{
    public class PhoneNumber : IEquatable<PhoneNumber>
    {
        public string Value { get; private set; }
        public string CountryCode { get; private set; }
        public string NationalNumber { get; private set; }

        private PhoneNumber() { } // EF Core için

        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Telefon numarası boş olamaz.");

            var cleaned = Regex.Replace(value, @"[^\d+]", "");

            if (!IsValidPhoneNumber(cleaned))
                throw new DomainException("Geçersiz telefon numarası formatı.");

            Value = cleaned;

            // Basit parse - gerçek hayatta libphonenumber kullanılabilir
            if (cleaned.StartsWith("+"))
            {
                var match = Regex.Match(cleaned, @"^\+(\d{1,3})(\d+)$");
                if (match.Success)
                {
                    CountryCode = "+" + match.Groups[1].Value;
                    NationalNumber = match.Groups[2].Value;
                }
                else
                {
                    CountryCode = cleaned.Substring(0, Math.Min(3, cleaned.Length));
                    NationalNumber = cleaned.Substring(CountryCode.Length);
                }
            }
            else
            {
                CountryCode = "+90"; // Varsayılan Türkiye
                NationalNumber = cleaned;
                Value = CountryCode + NationalNumber;
            }
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // En az 7, en fazla 15 rakam (uluslararası standart)
            var digits = Regex.Replace(phoneNumber, @"[^\d]", "");
            return digits.Length >= 7 && digits.Length <= 15;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PhoneNumber);
        }

        public bool Equals(PhoneNumber? other)
        {
            if (other is null)
                return false;

            return Value == other.Value;
        }

        /// <summary>
        /// Değer eşitliği. Operatör tanımlanmadığında '==' referans karşılaştırması
        /// yaptığı için aynı numarayı taşıyan iki PhoneNumber farklı sayılıyordu.
        /// </summary>
        /// <param name="left">Sol taraf.</param>
        /// <param name="right">Sağ taraf.</param>
        /// <returns>Numaralar aynıysa true.</returns>
        public static bool operator ==(PhoneNumber? left, PhoneNumber? right)
            => object.Equals(left, right);

        /// <summary>Değer eşitsizliği.</summary>
        /// <param name="left">Sol taraf.</param>
        /// <param name="right">Sağ taraf.</param>
        /// <returns>Numaralar farklıysa true.</returns>
        public static bool operator !=(PhoneNumber? left, PhoneNumber? right)
            => !object.Equals(left, right);

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(PhoneNumber phoneNumber)
        {
            return phoneNumber?.Value ?? string.Empty;
        }

        public static implicit operator PhoneNumber(string value)
        {
            return new PhoneNumber(value);
        }
    }
}