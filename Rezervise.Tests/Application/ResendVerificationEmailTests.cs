using Rezervise.Application.Common.Models;
using Rezervise.Application.Features.Users.Commands.ResendVerificationEmail;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Doğrulama e-postasını yeniden gönderme akışı: bekleme süresi
/// (<c>Email:ResendCooldownMinutes</c>, varsayılan 3 dakika) dolmadan yeni e-posta
/// kuyruğa verilmez ve istemciye kalan saniye döner.
/// </summary>
public class ResendVerificationEmailTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Resend_ShouldQueueEmail_WhenNoPreviousRequest()
    {
        var user = NewUser();
        var queue = new FakeEmailQueue();
        var sut = CreateSut(user, queue, Now);

        var result = await sut.Handle(
            new ResendVerificationEmailCommand { Email = user.Email }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(queue.Sent);
        Assert.Equal(user.Email, queue.Sent[0].To);
    }

    [Fact]
    public async Task Resend_ShouldNotQueueEmail_WhenWithinThreeMinutes()
    {
        var user = NewUser();
        // Son istek 1 dakika önce: 3 dakikalık bekleme dolmadı.
        user.IssueEmailVerificationToken("hash", Now.AddHours(24), Now.AddMinutes(-1));
        var queue = new FakeEmailQueue();
        var sut = CreateSut(user, queue, Now);

        var result = await sut.Handle(
            new ResendVerificationEmailCommand { Email = user.Email }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("TOO_MANY_REQUESTS", result.Error!.Code);
        Assert.Equal("Auth.ResendVerificationCooldown", result.Error.Key);
        Assert.InRange(result.Error.RetryAfterSeconds!.Value, 1, 180);
        Assert.Empty(queue.Sent);
    }

    [Fact]
    public async Task Resend_ShouldQueueEmail_WhenCooldownHasElapsed()
    {
        var user = NewUser();
        // Son istek 3 dakika 1 saniye önce: bekleme doldu.
        user.IssueEmailVerificationToken("hash", Now.AddHours(24), Now.AddMinutes(-3).AddSeconds(-1));
        var queue = new FakeEmailQueue();
        var sut = CreateSut(user, queue, Now);

        var result = await sut.Handle(
            new ResendVerificationEmailCommand { Email = user.Email }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(queue.Sent);
    }

    [Fact]
    public async Task Resend_ShouldStaySilent_WhenEmailIsUnknown()
    {
        var queue = new FakeEmailQueue();
        var sut = CreateSut(null, queue, Now);

        var result = await sut.Handle(
            new ResendVerificationEmailCommand { Email = "yok@example.com" }, CancellationToken.None);

        // Adresin kayıtlı olup olmadığı sızdırılmaz.
        Assert.True(result.IsSuccess);
        Assert.Empty(queue.Sent);
    }

    private static ResendVerificationEmailCommandHandler CreateSut(
        User? user, FakeEmailQueue queue, DateTime now)
        => new(
            new FakeRepository<User>(user is null ? Array.Empty<User>() : new[] { user }),
            new FakeUnitOfWork(),
            queue,
            new EmailOptions
            {
                VerificationLinkBaseUrl = "http://localhost/api/Auth/verify-email",
                ResendCooldownMinutes = 3
            },
            new StubLocalizationService(),
            new StubDateTimeService(now));

    private static User NewUser()
    {
        var user = new User(
            Guid.NewGuid(), "Test", "Kullanici",
            new Email($"resend-{Guid.NewGuid():N}@test.com"),
            new PhoneNumber("+905551112233"),
            EnumUserRole.Customer);

        user.ClearDomainEvents();
        return user;
    }
}
