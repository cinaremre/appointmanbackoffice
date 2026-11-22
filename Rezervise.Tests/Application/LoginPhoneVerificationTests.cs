using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Features.Users.Commands.Login;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Tests.Fakes;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Rezervise.Tests.Application;

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
        Assert.Contains("PhoneNotVerified", result.Error!.Message);
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
            new FakeTokenService(), new RezerviseMetrics());

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
