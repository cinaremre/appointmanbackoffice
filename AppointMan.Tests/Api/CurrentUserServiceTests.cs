using System.Security.Claims;
using AppointMan.Api.Services;
using Microsoft.AspNetCore.Http;

namespace AppointMan.Tests.Api;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_ShouldReadUserIdClaimAndRoles()
    {
        var userId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Role, "BusinessOwner"),
                new Claim(ClaimTypes.Role, "admin")
            },
            "TestAuthentication");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        Assert.True(service.IsAuthenticated);
        Assert.Equal(userId, service.UserId);
        Assert.True(service.IsInRole("BusinessOwner"));
        Assert.True(service.IsInRole("ADMIN"));
    }

    [Fact]
    public void UserId_ShouldBeNull_ForAnonymousRequest()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }
}
