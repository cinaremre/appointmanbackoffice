namespace AppointMan.Api.Authorization
{
    /// <summary>
    /// Policy names shared by the API surface. The role values match the strings
    /// written to the <c>role</c> claim by the token service.
    /// </summary>
    public static class AuthorizationPolicies
    {
        /// <summary>Any authenticated principal.</summary>
        public const string AuthenticatedUser = "AuthenticatedUser";

        /// <summary>Business owners only.</summary>
        public const string BusinessOwnerOnly = "BusinessOwnerOnly";

        /// <summary>Customers only.</summary>
        public const string CustomerOnly = "CustomerOnly";

        /// <summary>Administrators (Admin or SuperAdmin).</summary>
        public const string AdministratorOnly = "AdministratorOnly";
    }
}
