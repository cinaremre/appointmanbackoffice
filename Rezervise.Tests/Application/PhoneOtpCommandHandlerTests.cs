using Rezervise.Application.Common.Models;
using Rezervise.Application.Features.Users.Commands.ResendPhoneOtp;
using Rezervise.Application.Features.Users.Commands.VerifyPhoneOtp;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Infrastructure.Services.Sms;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Telefon OTP dogrulama ve yeniden gonderim akislari.
/// Kayit + OTP akisinda OTP dogrulandigi anda kullanici giris yapabilir.
/// </summary>
public class PhoneOtpCommandHandlerTests
{
    private const string Phone = "+905551112233";
    private const string FixedCode = "123456";

    private static readonly DateTime Now = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Verify_ShouldSucceed_WhenCodeMatches()
    {
        var user = NewUser();
        var otp = CreateOtp();
        otp.IssueCode(user, Now);
        var sut = new VerifyPhoneOtpCommandHandler(
            new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), otp, new StubDateTimeService(Now));

        var result = await sut.Handle(
            new VerifyPhoneOtpCommand { PhoneNumber = Phone, Code = FixedCode }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public async Task Verify_ShouldFail_WhenCodeIsWrong()
    {
        var user = NewUser();
        var otp = CreateOtp();
        otp.IssueCode(user, Now);
        var sut = new VerifyPhoneOtpCommandHandler(
            new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), otp, new StubDateTimeService(Now));

        var result = await sut.Handle(
            new VerifyPhoneOtpCommand { PhoneNumber = Phone, Code = "000000" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(user.IsPhoneVerified);
    }

    [Fact]
    public async Task Verify_ShouldFail_WhenPhoneIsNotRegistered()
    {
        var sut = new VerifyPhoneOtpCommandHandler(
            new FakeRepository<User>(), new FakeUnitOfWork(), CreateOtp(), new StubDateTimeService(Now));

        var result = await sut.Handle(
            new VerifyPhoneOtpCommand { PhoneNumber = "+905559998877", Code = FixedCode }, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Verify_ShouldFail_WhenCodeIsEmpty()
    {
        var user = NewUser();
        var sut = new VerifyPhoneOtpCommandHandler(
            new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), CreateOtp(), new StubDateTimeService(Now));

        var result = await sut.Handle(
            new VerifyPhoneOtpCommand { PhoneNumber = Phone, Code = " " }, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Resend_ShouldSendNewCode()
    {
        var user = NewUser();
        var sender = new FakeSmsCodeSender();
        var sut = CreateResendSut(user, sender);

        var result = await sut.Handle(new ResendPhoneOtpCommand { PhoneNumber = Phone }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, sender.SendCount);
        Assert.Equal(Phone, sender.Sent[0].PhoneNumber);
        Assert.Equal(FixedCode, sender.Sent[0].Code);
    }

    [Fact]
    public async Task Resend_ShouldNotSend_WhenWithinCooldown()
    {
        var user = NewUser();
        var otp = CreateOtp();
        otp.IssueCode(user, Now);
        var sender = new FakeSmsCodeSender();

        var sut = new ResendPhoneOtpCommandHandler(
            new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), otp, sender,
            new StubDateTimeService(Now), CreateOptions());

        var result = await sut.Handle(new ResendPhoneOtpCommand { PhoneNumber = Phone }, CancellationToken.None);

        // 3 dakikalik bekleme dolmadan yeni kod uretilmez; kalan sure istemciye doner.
        Assert.True(result.IsFailure);
        Assert.Equal("TOO_MANY_REQUESTS", result.Error!.Code);
        Assert.True(result.Error.RetryAfterSeconds > 0);
        Assert.Equal(0, sender.SendCount);
    }

    [Fact]
    public async Task Resend_ShouldSend_WhenCooldownHasElapsed()
    {
        var user = NewUser();
        var options = CreateOptions();
        var otp = new OtpCodeService(options);
        otp.IssueCode(user, Now);
        var sender = new FakeSmsCodeSender();

        var sut = new ResendPhoneOtpCommandHandler(
            new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), otp, sender,
            new StubDateTimeService(Now.AddSeconds(181)), options);

        var result = await sut.Handle(new ResendPhoneOtpCommand { PhoneNumber = Phone }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, sender.SendCount);
    }

    [Fact]
    public async Task Resend_ShouldStaySilent_WhenPhoneIsUnknown()
    {
        var sender = new FakeSmsCodeSender();
        var sut = new ResendPhoneOtpCommandHandler(
            new FakeRepository<User>(), new FakeUnitOfWork(), CreateOtp(), sender,
            new StubDateTimeService(Now), CreateOptions());

        var result = await sut.Handle(
            new ResendPhoneOtpCommand { PhoneNumber = "+905559998877" }, CancellationToken.None);

        // Numaranin kayitli olup olmadigi sizdirilmaz.
        Assert.True(result.IsSuccess);
        Assert.Equal(0, sender.SendCount);
    }

    [Fact]
    public async Task Resend_ShouldNotSend_WhenPhoneAlreadyVerified()
    {
        var user = NewUser();
        user.VerifyPhone();
        var sender = new FakeSmsCodeSender();
        var sut = CreateResendSut(user, sender);

        var result = await sut.Handle(new ResendPhoneOtpCommand { PhoneNumber = Phone }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, sender.SendCount);
    }

    private static ResendPhoneOtpCommandHandler CreateResendSut(User user, FakeSmsCodeSender sender)
        => new(new FakeRepository<User>(new[] { user }), new FakeUnitOfWork(), CreateOtp(), sender,
            new StubDateTimeService(Now), CreateOptions());

    private static OtpCodeService CreateOtp() => new(CreateOptions());

    private static OtpOptions CreateOptions() => new()
    {
        FixedCode = FixedCode,
        CodeLength = 6,
        LifetimeMinutes = 5,
        MaxAttempts = 5,
        ResendCooldownSeconds = 180
    };

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
