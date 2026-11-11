namespace AppointMan.Application.Common.Security
{
    /// <summary>
    /// Single source of truth for password complexity rules.
    /// Validators and authentication handlers must both consult this class so the policy
    /// cannot drift between the API surface and the persistence layer.
    /// </summary>
    public static class PasswordPolicy
    {
        /// <summary>Minimum accepted password length.</summary>
        public const int MinimumLength = 12;

        /// <summary>Maximum accepted password length; guards against hashing based denial of service.</summary>
        public const int MaximumLength = 128;

        /// <summary>True when at least one uppercase letter is required.</summary>
        public const bool RequireUppercase = true;

        /// <summary>True when at least one lowercase letter is required.</summary>
        public const bool RequireLowercase = true;

        /// <summary>True when at least one digit is required.</summary>
        public const bool RequireDigit = true;

        /// <summary>True when at least one non alphanumeric character is required.</summary>
        public const bool RequireNonAlphanumeric = true;

        /// <summary>
        /// Evaluates whether the supplied password satisfies the full complexity policy.
        /// </summary>
        /// <param name="password">Candidate password; may be null.</param>
        /// <returns><c>true</c> when the password is acceptable.</returns>
        public static bool IsStrong(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password.Length < MinimumLength || password.Length > MaximumLength)
                return false;

            var hasUpper = false;
            var hasLower = false;
            var hasDigit = false;
            var hasSymbol = false;

            foreach (var character in password)
            {
                if (char.IsUpper(character))
                    hasUpper = true;
                else if (char.IsLower(character))
                    hasLower = true;
                else if (char.IsDigit(character))
                    hasDigit = true;
                else
                    hasSymbol = true;
            }

            return hasUpper == RequireUppercase
                && hasLower == RequireLowercase
                && hasDigit == RequireDigit
                && hasSymbol == RequireNonAlphanumeric;
        }
    }
}
