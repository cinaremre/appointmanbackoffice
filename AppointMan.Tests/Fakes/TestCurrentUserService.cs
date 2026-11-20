using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Tests.Fakes;

public sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
    public bool IsAuthenticated { get; set; } = true;
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();

    public static TestCurrentUserService For(Guid userId, params string[] roles)
        => new() { UserId = userId, IsAuthenticated = true, Roles = roles };

    public bool IsInRole(string role)
        => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}
