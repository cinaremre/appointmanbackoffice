using System;
using System.Linq;
using System.Text.RegularExpressions;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.ValueObjects
{
    public class PhoneNumber : IEquatable<PhoneNumber>
    {
        /// <summary>
        /// Ülke kodu belirtilmeyen girdilerde kabul edilen varsayılan ülke kodu (+90 / Türkiye).
        /// </summary>
        private const string DefaultCountryCode = "+90";

        /// <summary>
        /// Kanonikleştirmede tanınan ülke kodları ("+" işareti olmadan, yalnızca rakam).
        /// En uzun kod önce denenir; bu sayede "+905551112233" girdisinde ülke kodu
        /// yanlışlıkla "905" olarak ayrılmaz.
        /// </summary>
        private static readonly string[] KnownCountryCodes = new[]
        {
            "1", "7", "20", "27", "30", "31", "32", "33", "34", "36",
            "39", "40", "41", "43", "44", "45", "46", "47", "48", "49",
            "51", "52", "53", "54", "55", "56", "57", "58", "60", "61",
            "62", "63", "64", "65", "66", "81", "82", "84", "86", "90",
            "91", "92", "93", "94", "95", "98",
            "212", "213", "216", "218", "220", "221", "234",
            "351", "352", "353", "356", "357", "358", "359",
            "370", "371", "372", "380", "381", "385", "386", "387",
            "420", "421", "423",
            "961", "962", "963", "964", "965", "966", "967", "968",
            "970", "971", "972", "973", "974", "975", "976", "977",
            "992", "993", "994", "995", "996", "998"
        }.OrderByDescending(code => code.Length).ToArray();

        public string Value { get; private set; }
        public string CountryCode { get; private set; }
        public string NationalNumber { get; private set; }

        private PhoneNumber() { } // EF Core için

        /// <summary>
        /// Telefon numarasını E.164 benzeri kanonik biçime getirir. Aynı numaranın
        /// "05551112233", "5551112233", "+90 555 111 22 33" gibi farklı yazımları tek bir
        /// <see cref="Value"/> üretir; kayıt akışındaki tekillik kontrolü bu değere dayanır.
        /// </summary>
        /// <param name="value">Ham telefon numarası.</param>
        /// <exception cref="DomainException">Numara boş veya geçersiz formatta ise fırlatılır.</exception>
        public PhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Telefon numarası boş olamaz.");

            var cleaned = Regex.Replace(value, @"[^\d+]", "");

            // "+" yalnızca numaranın başında anlamlıdır; arada kalan işaretler atılır.
            var hasPlusPrefix = cleaned.StartsWith("+");
            if (!hasPlusPrefix)
                cleaned = cleaned.Replace("+", "");

            if (!IsValidPhoneNumber(cleaned))
                throw new DomainException("Geçersiz telefon numarası formatı.");

            var digits = cleaned.TrimStart('+');

            // "00" uluslararası öneki "+" ile eşdeğer kabul edilir.
            var isInternational = hasPlusPrefix;
            if (digits.StartsWith("00", StringComparison.Ordinal))
            {
                digits = digits.Substring(2);
                isInternational = true;
            }

            // "+" yazılmadan ülke kodu girilmiş olabilir: TR ulusal numaraları 9 ile
            // başlamadığı için 12+ haneli "90..." girdisini ülke kodlu saymak güvenlidir.
            if (!isInternational && digits.Length >= 12 && digits.StartsWith("90", StringComparison.Ordinal))
                isInternational = true;

            string countryCode;
            string nationalNumber;

            if (isInternational)
            {
                // En uzun eşleşen kod kazanır: "905551112233" -> "90", "15551234567" -> "1".
                var countryCodeDigits = KnownCountryCodes.FirstOrDefault(
                    code => digits.Length > code.Length && digits.StartsWith(code, StringComparison.Ordinal))
                    ?? digits.Substring(0, Math.Min(3, digits.Length - 1));

                countryCode = "+" + countryCodeDigits;
                nationalNumber = digits.Substring(countryCodeDigits.Length);
            }
            else
            {
                countryCode = DefaultCountryCode;
                nationalNumber = digits;
            }

            // Ulusal numaranın başındaki trunk sıfırları kanonik biçimde yer almaz:
            // "0555 111 22 33" ve "+90 0555 111 22 33" aynı değere inmeli.
            nationalNumber = nationalNumber.TrimStart('0');

            // Toplam hane sayısı uluslararası standart aralıkta olmalı (7-15).
            var digitCount = countryCode.Length - 1 + nationalNumber.Length;
            if (nationalNumber.Length == 0 || digitCount < 7 || digitCount > 15)
                throw new DomainException("Geçersiz telefon numarası formatı.");

            CountryCode = countryCode;
            NationalNumber = nationalNumber;
            Value = countryCode + nationalNumber;
        }

        /// <summary>
        /// Girdinin uzunluk açısından makul bir telefon numarası olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="phoneNumber">Temizlenmiş numara.</param>
        /// <returns>7-15 hane aralığındaysa true.</returns>
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

        /// <summary>
        /// Değer eşitsizliği.
        /// </summary>
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
