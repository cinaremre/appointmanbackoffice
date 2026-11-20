using AppointMan.Application.Common.Models;
using AppointMan.Application.Common.Security;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;
using AppointMan.Infrastructure.Services.Sms;
using AppointMan.Tests;

namespace AppointMan.Tests.Application;

/// <summary>
/// Telefon dogrulama kodu uretimi/dogrulamasi: sabit kod, hash saklama,
/// sure ve deneme limiti kurallari.
/// </summary>
public class OtpCodeServiceTests
{
    private const string FixedCode = "123456";
    private const string Phone = "+905551112233";

    private static readonly DateTime Now = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IssueCode_ShouldReturnFixedCode_WhenConfigured()
    {
        var user = NewUser();
        var sut = CreateSut();

        var code = sut.IssueCode(user, Now);

        Assert.Equal(FixedCode, code);
    }

    [Fact]
    public void IssueCode_ShouldStoreHashOnly_AndResetAttempts()
    {
        var user = NewUser();
        user.IncrementPhoneVerificationAttempt();
        var sut = CreateSut();

        sut.IssueCode(user, Now);

        Assert.NotNull(user.PhoneVerificationCodeHash);
        Assert.NotEqual(FixedCode, user.PhoneVerificationCodeHash);
        Assert.Equal(RefreshTokenHasher.Hash(FixedCode), user.PhoneVerificationCodeHash);
        Assert.Equal(0, user.PhoneVerificationAttemptCount);
        Assert.Equal(Now.AddMinutes(5), user.PhoneVerificationCodeExpiresAt);
    }

    [Fact]
    public void VerifyCode_ShouldSucceed_AndMarkPhoneVerified()
    {
        var user = NewUser();
        var sut = CreateSut();
        sut.IssueCode(user, Now);

        var (success, errorKey) = sut.VerifyCode(user, FixedCode, Now);

        Assert.True(success);
        Assert.Null(errorKey);
        Assert.True(user.IsPhoneVerified);
        Assert.Null(user.PhoneVerificationCodeHash);
    }

    [Fact]
    public void VerifyCode_ShouldFail_AndIncrementAttempts_WhenCodeIsWrong()
    {
        var user = NewUser();
        var sut = CreateSut();
        sut.IssueCode(user, Now);

        var (success, errorKey) = sut.VerifyCode(user, "000000", Now);

        Assert.False(success);
        Assert.Equal("Otp.InvalidCode", errorKey);
        Assert.False(user.IsPhoneVerified);
        Assert.Equal(1, user.PhoneVerificationAttemptCount);
    }

    [Fact]
    public void VerifyCode_ShouldFail_WhenCodeExpired()
    {
        var user = NewUser();
        user.IssuePhoneVerificationCode(
            RefreshTokenHasher.Hash("999999"), Now.AddMinutes(-1), Now.AddMinutes(-10));
        var sut = CreateSut();

        var (success, errorKey) = sut.VerifyCode(user, "999999", Now);

        Assert.False(success);
        Assert.Equal("Otp.Expired", errorKey);
    }

    [Fact]
    public void VerifyCode_ShouldFail_WhenAttemptLimitReached()
    {
        var user = NewUser();
        var sut = CreateSut();
        sut.IssueCode(user, Now);

        for (var i = 0; i < 5; i++)
            user.IncrementPhoneVerificationAttempt();

        var (success, errorKey) = sut.VerifyCode(user, FixedCode, Now);

        Assert.False(success);
        Assert.Equal("Otp.TooManyAttempts", errorKey);
        Assert.False(user.IsPhoneVerified);
    }

    [Fact]
    public void VerifyCode_ShouldFail_WhenNoCodeWasIssued()
    {
        var user = NewUser();
        var sut = CreateSut();

        var (success, errorKey) = sut.VerifyCode(user, FixedCode, Now);

        Assert.False(success);
        Assert.Equal("Otp.CodeNotIssued", errorKey);
    }

    [Fact]
    public void VerifyCode_ShouldFail_WhenCodeIsEmpty()
    {
        var user = NewUser();
        var sut = CreateSut();
        sut.IssueCode(user, Now);

        var (success, errorKey) = sut.VerifyCode(user, "  ", Now);

        Assert.False(success);
        Assert.Equal("Otp.CodeRequired", errorKey);
    }

    private static OtpCodeService CreateSut() => new(new OtpOptions
    {
        FixedCode = FixedCode,
        CodeLength = 6,
        LifetimeMinutes = 5,
        MaxAttempts = 5,
        ResendCooldownSeconds = 60
    });

    private static User NewUser()
    {
        var user = new User(
            Guid.NewGuid(), "Test", "Kullanici",
            new Email($"otp-{Guid.NewGuid():N}@test.com"),
            new PhoneNumber(Phone),
            EnumUserRole.Customer);

        user.ClearDomainEvents();
        return user;
    }
}
