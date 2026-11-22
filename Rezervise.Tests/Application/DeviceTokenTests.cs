using Rezervise.Application.Common.Models;
using Rezervise.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;
using Rezervise.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Cihaz token kayit akisi: kendi cihazini kaydetme (upsert), baskasi adina
/// kayit reddi (IDOR), gecersiz token dogrulamasi ve pasiflestirme.
/// </summary>
public sealed class DeviceTokenTests
{
    private const string ValidToken = "fcm-token-";
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static string Token(int length = 64) => ValidToken.PadRight(length, 'x');

    private static (RegisterDeviceTokenCommandHandler Handler, FakeRepository<DeviceToken> Repo) CreateSut(
        TestCurrentUserService? currentUser = null)
    {
        var repo = new FakeRepository<DeviceToken>();
        var handler = new RegisterDeviceTokenCommandHandler(
            repo,
            new FakeUnitOfWork(),
            currentUser ?? TestCurrentUserService.For(OwnerId),
            new StubDateTimeService(DateTime.UtcNow));
        return (handler, repo);
    }

    [Fact]
    public async Task Register_ShouldSaveOwnDeviceToken()
    {
        var (sut, repo) = CreateSut();

        var result = await sut.Handle(new RegisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = Token(),
            Platform = EnumDevicePlatform.Android,
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
        Assert.True(repo.Items[0].IsActive);
    }

    [Fact]
    public async Task Register_ShouldBeIdempotent_WhenSameTokenReRegistered()
    {
        var (sut, repo) = CreateSut();
        var token = Token();

        var first = await sut.Handle(new RegisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = token,
            Platform = EnumDevicePlatform.Ios,
        }, CancellationToken.None);

        var second = await sut.Handle(new RegisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = token,
            Platform = EnumDevicePlatform.Ios,
        }, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        // Ayni token ikinci kayitta yeni satir acmaz; gorunurlugu tazeler.
        Assert.Single(repo.Items);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task Register_ShouldReject_WhenCalledForAnotherUser()
    {
        var (sut, repo) = CreateSut();

        var result = await sut.Handle(new RegisterDeviceTokenCommand
        {
            UserId = Guid.NewGuid(), // baskasinin hesabi
            Token = Token(),
            Platform = EnumDevicePlatform.Android,
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Register_ShouldReject_TooShortToken()
    {
        var (sut, _) = CreateSut();

        var result = await sut.Handle(new RegisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = "short",
            Platform = EnumDevicePlatform.Android,
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    [Fact]
    public async Task Unregister_ShouldDeactivateOwnToken()
    {
        var repo = new FakeRepository<DeviceToken>();
        var entry = new DeviceToken(OwnerId, Token(), EnumDevicePlatform.Android, DateTime.UtcNow);
        await repo.AddAsync(entry);

        var handler = new UnregisterDeviceTokenCommandHandler(
            repo,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(new UnregisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = entry.Token,
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(entry.IsActive);
    }

    [Fact]
    public async Task Unregister_ShouldReject_WhenCalledForAnotherUser()
    {
        var repo = new FakeRepository<DeviceToken>();
        var entry = new DeviceToken(OwnerId, Token(), EnumDevicePlatform.Android, DateTime.UtcNow);
        await repo.AddAsync(entry);

        var handler = new UnregisterDeviceTokenCommandHandler(
            repo,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(Guid.NewGuid()));

        var result = await handler.Handle(new UnregisterDeviceTokenCommand
        {
            UserId = OwnerId,
            Token = entry.Token,
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.True(entry.IsActive);
    }
}
