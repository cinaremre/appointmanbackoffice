using AppointMan.Application.Common.Authorization;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

public class CallerAuthorizationTests
{
    [Fact]
    public void CanActAsUser_ShouldAllowOnlySelfOrAdmin()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        Assert.True(CallerAuthorization.CanActAsUser(TestCurrentUserService.For(userId), userId));
        Assert.False(CallerAuthorization.CanActAsUser(TestCurrentUserService.For(otherId), userId));
        Assert.True(CallerAuthorization.CanActAsUser(TestCurrentUserService.For(otherId, "Admin"), userId));
    }

    [Fact]
    public void CanActForBusiness_ShouldAllowOwnerOrAdmin()
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        Assert.True(CallerAuthorization.CanActForBusiness(TestCurrentUserService.For(ownerId), ownerId));
        Assert.False(CallerAuthorization.CanActForBusiness(TestCurrentUserService.For(callerId), ownerId));
        Assert.True(CallerAuthorization.CanActForBusiness(
            TestCurrentUserService.For(callerId, "SuperAdmin"), ownerId));
    }
}
