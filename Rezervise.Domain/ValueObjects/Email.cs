using System;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.ValueObjects
{
    public class Email : IEquatable<Email>
    {
        public string Value { get; private set; }

        private Email() { } // EF Core için

        public Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Email boş olamaz.");

            if (!IsValidEmail(value))
                throw new DomainException("Geçersiz email format.");

            Value = value.ToLowerInvariant().Trim();
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Email);
        }

        public bool Equals(Email? other)
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
            return Value;
        }

        public static implicit operator string(Email email)
        {
            return email?.Value ?? string.Empty;
        }

        public static implicit operator Email(string value)
        {
            return new Email(value);
        }
    }
}