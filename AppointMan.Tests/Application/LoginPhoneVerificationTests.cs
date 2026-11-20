using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Features.Users.Commands.Login;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;
using AppointMan.Tests.Fakes;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AppointMan.Tests.Application;

/// <summary>
/// Giris kapisi: e-posta dogrulanmamis olsa bile telefon (OTP) dogrulanmissa
/// giris yapilabilir; ikisi de dogrulanmamissa giris engellenir.
/// </summary>
public class LoginPhoneVerificationTests
{
    private const string Password = "Passw0rd!23";

    [Fact]
    public async Task Handle_ShouldSucceed_WhenOnlyPhoneIsVerified()
    {
        var user = NewUser();
        user.VerifyPhone();
        var sut = CreateSut(user);

        var result = await sut.Handle(NewCommand(user), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(user.IsEmailVerified);
        Assert.True(user.IsPhoneVerified);
        Assert.Equal(user.Id, result.Value.UserId);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNeitherEmailNorPhoneIsVerified()
    {
        var user = NewUser();
        var sut = CreateSut(user);

        var result = await sut.Handle(NewCommand(user), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("EmailNotVerified", result.Error!.Message);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenPasswordIsWrong()
    {
        var user = NewUser();
        user.VerifyPhone();
        var sut = CreateSut(user);

        var command = NewCommand(user);
        command.Password = "WrongPassword!1";

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("InvalidCredentials", result.Error!.Message);
    }

    private static LoginCommand NewCommand(User user) => new()
    {
        Email = user.Email,
        Password = Password
    };

    private static LoginCommandHandler CreateSut(User user)
        => new(new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(),
            new FakeTokenService(), new AppointManMetrics());

    private static User NewUser()
    {
        var user = new User(
            Guid.NewGuid(), "Test", "Kullanici",
            new Email($"login-{Guid.NewGuid():N}@test.com"),
            new PhoneNumber($"+9055{Guid.NewGuid().ToString("N")[..8]}"),
            EnumUserRole.Customer);

        user.SetPasswordHash(BCryptNet.HashPassword(Password));
        user.ClearDomainEvents();

        return user;
    }
}
