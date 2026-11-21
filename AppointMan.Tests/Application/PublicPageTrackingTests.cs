using AppointMan.Application.Features.PublicPages.Commands.PublishPublicPage;
using AppointMan.Application.Features.PublicPages.Queries.GetPublicPageBySlug;
using AppointMan.Application.Features.Tracking.Commands.CreateTrackingLink;
using AppointMan.Application.Features.Tracking.Commands.RecordTrackingEvent;
using AppointMan.Application.Features.Tracking.Queries.GetTrackingLinkStats;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Public sayfa ve anonim takip olcumu:
/// slug kurallari, slug cakismasi, anonim erisim, sessiz basari ve
/// istatistik kirilimlari.
/// </summary>
public class PublicPageTrackingTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();

    // ===== Public page yayinlama =====

    [Fact]
    public async Task Publish_ShouldCreatePublishedPage_WhenNoneExists()
    {
        var (sut, pages, _) = CreatePublishSut();

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = "benim-salonum"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(pages.Items);
        Assert.Equal("benim-salonum", pages.Items[0].Slug);
        Assert.True(pages.Items[0].IsPublished);
        Assert.Equal(pages.Items[0].Id, result.Value);
    }

    [Fact]
    public async Task Publish_ShouldUpdateExistingPage_OfSameBusiness()
    {
        // Isletmeye ait tek sayfa: varsa guncellenmeli, yenisi acilmamali.
        var existing = new BusinessPublicPage(BusinessId, "ilk-slug", isPublished: false);
        existing.ClearDomainEvents();

        var pages = new FakeRepository<BusinessPublicPage>(new[] { existing });
        var (sut, _, _) = CreatePublishSut(pages);

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = "yeni-slug"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(pages.Items);
        Assert.Equal(existing.Id, result.Value);
        Assert.Equal("yeni-slug", existing.Slug);
        Assert.True(existing.IsPublished);
    }

    [Fact]
    public async Task Publish_ShouldReturnConflict_WhenSlugOwnedByAnotherBusiness()
    {
        var otherPage = new BusinessPublicPage(OtherBusinessId, "ortak-slug");
        otherPage.ClearDomainEvents();

        var pages = new FakeRepository<BusinessPublicPage>(new[] { otherPage });
        var (sut, pageRepo, _) = CreatePublishSut(pages);

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = "ortak-slug"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        // Yeni sayfa acilmamali; diger isletmenin sayfasi degismemeli.
        Assert.Single(pageRepo.Items);
        Assert.Equal(OtherBusinessId, pageRepo.Items[0].BusinessId);
    }

    [Theory]
    [InlineData("Gecersiz Slug!")]
    [InlineData("slug_icinde_altcizgi")]
    [InlineData("slug.nokta")]
    [InlineData("a")]
    [InlineData("bu-slug-kesinlikle-altmis-karakterden-uzun-olacak-gibi-duruyor")]
    public async Task Publish_ShouldFailWithBusinessRule_WhenSlugInvalid(string slug)
    {
        var (sut, pages, _) = CreatePublishSut();

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = slug
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(pages.Items);
    }

    [Fact]
    public async Task Publish_ShouldNormalizeSlug_ToLowerCase()
    {
        var (sut, pages, _) = CreatePublishSut();

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = "Benim-Salonum"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("benim-salonum", pages.Items[0].Slug);
    }

    [Fact]
    public async Task Publish_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var (sut, pages, _) = CreatePublishSut(currentUser: TestCurrentUserService.For(Guid.NewGuid()));

        var result = await sut.Handle(new PublishPublicPageCommand
        {
            BusinessId = BusinessId,
            Slug = "benim-salonum"
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(pages.Items);
    }

    // ===== Anonim slug sorgusu =====

    [Fact]
    public async Task GetBySlug_ShouldReturnPageAndActiveServices_WhenPublished()
    {
        var serviceId = Guid.NewGuid();
        var page = new BusinessPublicPage(BusinessId, "benim-salonum");
        page.ClearDomainEvents();

        var sut = new GetPublicPageBySlugQueryHandler(
            new FakeRepository<BusinessPublicPage>(new[] { page }),
            BusinessRepo(),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, serviceId) }));

        var result = await sut.Handle(new GetPublicPageBySlugQuery { Slug = "benim-salonum" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Isletmesi", result.Value.BusinessName);
        Assert.Single(result.Value.Services);
        Assert.Equal(serviceId, result.Value.Services[0].Id);
        Assert.Equal(100m, result.Value.Services[0].Price);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnNotFound_WhenPageNotPublished()
    {
        // Yayinda olmayan sayfa varlik bilgisi sizdirmamali: NotFound.
        var page = new BusinessPublicPage(BusinessId, "gizli-slug", isPublished: false);
        page.ClearDomainEvents();

        var sut = new GetPublicPageBySlugQueryHandler(
            new FakeRepository<BusinessPublicPage>(new[] { page }),
            BusinessRepo(),
            new FakeRepository<Service>());

        var result = await sut.Handle(new GetPublicPageBySlugQuery { Slug = "gizli-slug" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnNotFound_WhenPageMissing()
    {
        var sut = new GetPublicPageBySlugQueryHandler(
            new FakeRepository<BusinessPublicPage>(),
            BusinessRepo(),
            new FakeRepository<Service>());

        var result = await sut.Handle(new GetPublicPageBySlugQuery { Slug = "yok-boyle-slug" }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Takip linki olusturma =====

    [Fact]
    public async Task CreateTrackingLink_ShouldGenerateUniqueShortCode()
    {
        var (sut, links, _) = CreateTrackingSut();

        var first = await sut.Handle(NewTrackingCommand(), CancellationToken.None);
        var second = await sut.Handle(NewTrackingCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(8, first.Value.Length);
        Assert.Equal(8, second.Value.Length);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, links.Items.Count);
        Assert.True(links.Items[0].IsActive);
    }

    [Fact]
    public async Task CreateTrackingLink_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var (sut, links, _) = CreateTrackingSut(currentUser: TestCurrentUserService.For(Guid.NewGuid()));

        var result = await sut.Handle(NewTrackingCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(links.Items);
    }

    // ===== Anonim olay kaydi =====

    [Fact]
    public async Task RecordTrackingEvent_ShouldStoreEvent_WhenTrackingCodeExists()
    {
        var links = new FakeRepository<TrackingLink>();
        var events = new FakeRepository<TrackingEvent>();
        var createSut = new CreateTrackingLinkCommandHandler(links, BusinessRepo(), new FakeUnitOfWork(), CurrentUser());

        var code = (await createSut.Handle(NewTrackingCommand(), CancellationToken.None)).Value;

        var recordSut = new RecordTrackingEventCommandHandler(
            links, new FakeRepository<BusinessPublicPage>(), events, new FakeUnitOfWork());

        var result = await recordSut.Handle(new RecordTrackingEventCommand
        {
            TrackingCode = code,
            EventType = EnumTrackingEventType.LinkOpened,
            AnonymousSessionId = Guid.NewGuid()
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Single(events.Items);
        Assert.Equal(links.Items[0].Id, events.Items[0].TrackingLinkId);
        Assert.Equal(BusinessId, events.Items[0].BusinessId);
    }

    [Fact]
    public async Task RecordTrackingEvent_ShouldReturnSilentSuccess_WhenCodeUnknown()
    {
        // Bilinmeyen kod hedefin varligini ele vermemeli: sessiz basari.
        var events = new FakeRepository<TrackingEvent>();
        var sut = new RecordTrackingEventCommandHandler(
            new FakeRepository<TrackingLink>(),
            new FakeRepository<BusinessPublicPage>(),
            events,
            new FakeUnitOfWork());

        var result = await sut.Handle(new RecordTrackingEventCommand
        {
            TrackingCode = "yokboyle",
            EventType = EnumTrackingEventType.LinkOpened
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Empty(events.Items);
    }

    [Fact]
    public async Task RecordTrackingEvent_ShouldStoreEvent_WhenSlugProvided()
    {
        var page = new BusinessPublicPage(BusinessId, "benim-salonum");
        page.ClearDomainEvents();

        var events = new FakeRepository<TrackingEvent>();
        var sut = new RecordTrackingEventCommandHandler(
            new FakeRepository<TrackingLink>(),
            new FakeRepository<BusinessPublicPage>(new[] { page }),
            events,
            new FakeUnitOfWork());

        var result = await sut.Handle(new RecordTrackingEventCommand
        {
            Slug = "benim-salonum",
            EventType = EnumTrackingEventType.ServiceViewed
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(events.Items);
        Assert.Equal(BusinessId, events.Items[0].BusinessId);
        Assert.Null(events.Items[0].TrackingLinkId);
    }

    [Fact]
    public async Task RecordTrackingEvent_ShouldFailValidation_WhenCodeAndSlugMissing()
    {
        var sut = new RecordTrackingEventCommandHandler(
            new FakeRepository<TrackingLink>(),
            new FakeRepository<BusinessPublicPage>(),
            new FakeRepository<TrackingEvent>(),
            new FakeUnitOfWork());

        var result = await sut.Handle(new RecordTrackingEventCommand
        {
            EventType = EnumTrackingEventType.LinkOpened
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
    }

    // ===== Istatistik =====

    [Fact]
    public async Task GetTrackingLinkStats_ShouldReportOpensSessionsAndEventBreakdown()
    {
        var links = new FakeRepository<TrackingLink>();
        var events = new FakeRepository<TrackingEvent>();
        var unitOfWork = new FakeUnitOfWork();

        var createSut = new CreateTrackingLinkCommandHandler(links, BusinessRepo(), unitOfWork, CurrentUser());
        var recordSut = new RecordTrackingEventCommandHandler(
            links, new FakeRepository<BusinessPublicPage>(), events, unitOfWork);

        var code = (await createSut.Handle(NewTrackingCommand(), CancellationToken.None)).Value;

        // 3 acilis (2 farkli oturum + 1 oturumsuz) + 1 hizmet goruntuleme.
        var sessionA = Guid.NewGuid();
        var sessionB = Guid.NewGuid();

        await recordSut.Handle(Event(code, EnumTrackingEventType.LinkOpened, sessionA), CancellationToken.None);
        await recordSut.Handle(Event(code, EnumTrackingEventType.LinkOpened, sessionB), CancellationToken.None);
        await recordSut.Handle(Event(code, EnumTrackingEventType.LinkOpened), CancellationToken.None);
        await recordSut.Handle(Event(code, EnumTrackingEventType.ServiceViewed, sessionA), CancellationToken.None);

        var statsSut = new GetTrackingLinkStatsQueryHandler(links, events, BusinessRepo(), CurrentUser());

        var result = await statsSut.Handle(new GetTrackingLinkStatsQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stats = Assert.Single(result.Value);
        Assert.Equal(3, stats.TotalOpens);
        Assert.Equal(2, stats.UniqueSessions);
        Assert.Contains(stats.EventCounts, c => c.EventType == EnumTrackingEventType.LinkOpened && c.Count == 3);
        Assert.Contains(stats.EventCounts, c => c.EventType == EnumTrackingEventType.ServiceViewed && c.Count == 1);
    }

    [Fact]
    public async Task GetTrackingLinkStats_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var statsSut = new GetTrackingLinkStatsQueryHandler(
            new FakeRepository<TrackingLink>(),
            new FakeRepository<TrackingEvent>(),
            BusinessRepo(),
            TestCurrentUserService.For(Guid.NewGuid()));

        var result = await statsSut.Handle(new GetTrackingLinkStatsQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    // ===== Yardimcilar =====

    private static TestCurrentUserService CurrentUser()
        => TestCurrentUserService.For(OwnerId);

    private static FakeRepository<Business> BusinessRepo()
        => new(new[]
        {
            TestData.NewBusiness(OwnerId, BusinessId),
            TestData.NewBusiness(Guid.NewGuid(), OtherBusinessId)
        });

    private static (PublishPublicPageCommandHandler Sut, FakeRepository<BusinessPublicPage> Pages, FakeUnitOfWork UnitOfWork)
        CreatePublishSut(FakeRepository<BusinessPublicPage>? pages = null, TestCurrentUserService? currentUser = null)
    {
        var pageRepo = pages ?? new FakeRepository<BusinessPublicPage>();
        var unitOfWork = new FakeUnitOfWork();

        return (
            new PublishPublicPageCommandHandler(pageRepo, BusinessRepo(), unitOfWork, currentUser ?? TestCurrentUserService.For(OwnerId)),
            pageRepo,
            unitOfWork);
    }

    private static (CreateTrackingLinkCommandHandler Sut, FakeRepository<TrackingLink> Links, FakeUnitOfWork UnitOfWork)
        CreateTrackingSut(TestCurrentUserService? currentUser = null)
    {
        var links = new FakeRepository<TrackingLink>();
        var unitOfWork = new FakeUnitOfWork();

        return (
            new CreateTrackingLinkCommandHandler(links, BusinessRepo(), unitOfWork, currentUser ?? TestCurrentUserService.For(OwnerId)),
            links,
            unitOfWork);
    }

    private static CreateTrackingLinkCommand NewTrackingCommand() => new()
    {
        BusinessId = BusinessId,
        TargetType = EnumTrackingTargetType.Business,
        Source = "qr",
        DestinationPath = "/p/benim-salonum"
    };

    private static RecordTrackingEventCommand Event(
        string code,
        EnumTrackingEventType eventType,
        Guid? sessionId = null) => new()
    {
        TrackingCode = code,
        EventType = eventType,
        AnonymousSessionId = sessionId
    };
}
