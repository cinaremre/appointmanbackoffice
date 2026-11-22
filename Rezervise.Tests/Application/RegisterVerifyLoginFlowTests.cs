using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Features.Users.Commands.Login;
using Rezervise.Application.Features.Users.Commands.RegisterUser;
using Rezervise.Application.Features.Users.Commands.VerifyPhoneOtp;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Infrastructure.Services.Sms;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Kayit -> OTP dogrulama -> giris akisinin tamami. Test ortaminda SMS
/// saglayicisi olmadigi icin sabit kod ("123456") kullanilir ve e-posta
/// dogrulanmamis olsa bile OTP tek basina girise yeter.
/// </summary>
public sealed class RegisterVerifyLoginFlowTests
{
    private const string Password = "Passw0rd!23";
    private const string TestOtp = "123456";
    private const string Phone = "+905333502650";
    private const string FirstName = "İbrahim";
    private const string LastName = "Aydın";

    [Fact]
    public async Task Register_ThenVerifyOtp_ThenLogin_SucceedsWithoutEmailVerification()
    {
        var users = new FakeRepository<User>();
        var unitOfWork = new FakeUnitOfWork();
        var otpOptions = new OtpOptions
        {
            FixedCode = TestOtp,
            CodeLength = 6,
            LifetimeMinutes = 5,
            MaxAttempts = 5,
            ResendCooldownSeconds = 60
        };
        var otpService = new OtpCodeService(otpOptions);
        var smsSender = new FakeSmsCodeSender();
        var emailQueue = new RecordingEmailQueue();
        var emailOptions = new EmailOptions
        {
            VerificationLinkBaseUrl = "http://localhost/api/Auth/verify-email"
        };
        var metrics = new RezerviseMetrics();
        var dateTimeService = new StubDateTimeService(DateTime.UtcNow);

        // Log'dakiyle ayni yapida veri: Turkce karakterli ad, gmail adresi, +90'li telefon.
        var email = $"ibay9206+{Guid.NewGuid():N}@gmail.com";

        // 1) Register
        var registerHandler = new RegisterUserCommandHandler(
            users,
            unitOfWork,
            metrics,
            emailQueue,
            emailOptions,
            new StubLocalizationService(),
            otpService,
            smsSender,
            dateTimeService);

        var register = await registerHandler.Handle(new RegisterUserCommand
        {
            FirstName = FirstName,
            LastName = LastName,
            Email = email,
            PhoneNumber = Phone,
            Password = Password,
            Role = EnumUserRole.Customer
        }, CancellationToken.None);

        Assert.True(register.IsSuccess, register.Error?.Message);

        var user = Assert.Single(users.Items);
        Assert.False(user.IsEmailVerified);
        Assert.False(user.IsPhoneVerified);
        Assert.NotNull(user.PhoneVerificationCodeHash);
        Assert.Equal(TestOtp, Assert.Single(smsSender.Sent).Code);
        Assert.Single(emailQueue.Messages);

        // 2) OTP verify (mail dogrulanmadi; OTP tek basina yeterli olmali)
        var verifyHandler = new VerifyPhoneOtpCommandHandler(
            users,
            unitOfWork,
            otpService,
            dateTimeService);

        var verify = await verifyHandler.Handle(new VerifyPhoneOtpCommand
        {
            PhoneNumber = Phone,
            Code = TestOtp
        }, CancellationToken.None);

        Assert.True(verify.IsSuccess, verify.Error?.Message);
        Assert.True(user.IsPhoneVerified);
        Assert.False(user.IsEmailVerified);

        // 3) Login
        var loginHandler = new LoginCommandHandler(
            users,
            unitOfWork,
            new FakeTokenService(),
            metrics);

        var login = await loginHandler.Handle(new LoginCommand
        {
            Email = email,
            Password = Password
        }, CancellationToken.None);

        Assert.True(login.IsSuccess, login.Error?.Message);
        Assert.Equal(user.Id, login.Value.UserId);
        Assert.False(user.IsEmailVerified);
        Assert.True(user.IsPhoneVerified);
    }

    [Fact]
    public async Task Login_BeforeOtpVerification_Fails()
    {
        var user = new User(
            Guid.NewGuid(), FirstName, LastName,
            new Email($"pending-{Guid.NewGuid():N}@gmail.com"),
            new PhoneNumber(Phone),
            EnumUserRole.Customer);

        user.SetPasswordHash(BCrypt.Net.BCrypt.HashPassword(Password));
        user.ClearDomainEvents();

        var users = new FakeRepository<User>([user]);
        var sut = new LoginCommandHandler(
            users,
            new FakeUnitOfWork(),
            new FakeTokenService(),
            new RezerviseMetrics());

        var result = await sut.Handle(new LoginCommand
        {
            Email = user.Email,
            Password = Password
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("PhoneNotVerified", result.Error!.Message);
    }

    private sealed class RecordingEmailQueue : IEmailQueue
    {
        public List<EmailMessage> Messages { get; } = [];

        public bool TryEnqueue(EmailMessage message)
        {
            Messages.Add(message);
            return true;
        }
    }
}
