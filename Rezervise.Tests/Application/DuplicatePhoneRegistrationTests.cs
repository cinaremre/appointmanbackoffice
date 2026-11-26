using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Models;
using Rezervise.Application.Features.Users.Commands.RegisterUser;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Services.Sms;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Aynı telefon numarasının farklı yazımlarıyla ikinci kayıt ASLA oluşmamalı.
/// Kanonikleştirme (+905551112233) sayesinde "0555...", "+90 555..." ve
/// "0090 555..." aynı kayda çarpar.
/// </summary>
public class DuplicatePhoneRegistrationTests
{
    private const string Password = "Passw0rd!23";

    [Theory]
    [InlineData("05551112233")]
    [InlineData("5551112233")]
    [InlineData("+90 555 111 22 33")]
    [InlineData("+90 (0555) 111 22 33")]
    [InlineData("0090 555 111 22 33")]
    [InlineData("905551112233")]
    public async Task Register_SecondTimeWithDifferentFormat_ShouldReturnPhoneTaken(string secondFormat)
    {
        var repository = new FakeRepository<User>();
        var sut = CreateHandler(repository);

        var first = await sut.Handle(NewCommand("+905551112233", "ilk@example.com"), CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.Equal("+905551112233", Assert.Single(repository.Items).PhoneNumber.Value);

        var second = await sut.Handle(NewCommand(secondFormat, "ikinci@example.com"), CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Equal("User.PhoneTaken", second.Error.Message);
        Assert.Single(repository.Items);
    }

    private static RegisterUserCommandHandler CreateHandler(FakeRepository<User> repository)
    {
        var otpOptions = new OtpOptions
        {
            FixedCode = "123456",
            CodeLength = 6,
            LifetimeMinutes = 5,
            MaxAttempts = 5,
            ResendCooldownSeconds = 60
        };

        return new RegisterUserCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new RezerviseMetrics(),
            new FakeEmailQueue(),
            new EmailOptions { VerificationLinkBaseUrl = "http://localhost/api/Auth/verify-email" },
            new StubLocalizationService(),
            new OtpCodeService(otpOptions),
            new FakeSmsCodeSender(),
            new StubDateTimeService(DateTime.UtcNow));
    }

    private static RegisterUserCommand NewCommand(string phone, string email) => new()
    {
        FirstName = "Test",
        LastName = "Kullanici",
        Email = email,
        PhoneNumber = phone,
        Password = Password,
        Role = EnumUserRole.Customer
    };
}
