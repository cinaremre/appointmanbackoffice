namespace AppointMan.Application.Common.Security
{
    /// <summary>
    /// Brute force protection thresholds applied by the login flow.
    /// Kept in one place so the lockout contract is documented and testable.
    /// </summary>
    public static class AccountLockoutPolicy
    {
        /// <summary>Number of consecutive failed attempts before the account is locked.</summary>
        public const int MaxFailedAccessAttempts = 5;

        /// <summary>Duration the account stays locked after exceeding the attempt limit.</summary>
        public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    }
}
