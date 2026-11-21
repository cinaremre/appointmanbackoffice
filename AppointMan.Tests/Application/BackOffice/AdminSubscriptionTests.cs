using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.AssignSubscriptionByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.CreateSubscriptionPlanByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Commands.GrantFeatureOverrideByAdmin;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetAdminSubscriptionPlans;
using AppointMan.Application.Features.Admin.Subscriptions.Queries.GetBusinessSubscription;
using AppointMan.Application.Features.Subscriptions.Commands.AssignSubscription;
using AppointMan.Application.Features.Subscriptions.Commands.CreatePlan;
using AppointMan.Application.Features.Subscriptions.Commands.GrantFeatureOverride;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;
using MediatR;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Backoffice abonelik yonetimi (dokuman Modul 13) testleri:
/// plan listeleme, mevcut domain komutlarini sarmalayan admin
/// komutlari ve kritik islem denetimi (AdminAuditLogs) kayitlari.
/// </summary>
public class AdminSubscriptionTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly DateTime NowUtc = new(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);

    // ===== Fake'ler =====

    /// <summary>Kritik islem denetim kayitlarini bellekte toplayan fake.</summary>
    private sealed class FakeAdminAuditSink : IAdminAuditSink
    {
        public List<AuditEntry> Entries { get; } = new();

        public Task RecordAsync(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            Entries.Add(new AuditEntry
            {
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                BusinessId = businessId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason,
                CorrelationId = correlationId,
                IpAddress = ipAddress
            });

            return Task.CompletedTask;
        }

        public sealed class AuditEntry
        {
            public Guid ActorUserId { get; init; }
            public string ActorRole { get; init; } = string.Empty;
            public Guid? BusinessId { get; init; }
            public string EntityType { get; init; } = string.Empty;
            public string EntityId { get; init; } = string.Empty;
            public string Action { get; init; } = string.Empty;
            public string? OldValues { get; init; }
            public string? NewValues { get; init; }
            public string? Reason { get; init; }
            public string? CorrelationId { get; init; }
            public string? IpAddress { get; init; }
        }
    }

    /// <summary>Sabit denetim baglami: correlation ve IP degerleri testte bilinir.</summary>
    private sealed class FakeAdminAuditContext : IAdminAuditContext
    {
        public string CorrelationId => "test-correlation";
        public string IpAddress => "127.0.0.1";
    }

    /// <summary>
    /// IMediator fake: kayitli delegate iletileri isler; gonderilen
    /// komutlar Sent listesinde toplanir. Kullanilan imza
    /// Send(object, ct) uzerinden yurur, diger imzalar desteklenmez.
    /// </summary>
    private sealed class FakeMediator : IMediator
    {
        private readonly Func<object, Task<object?>> _send;

        public FakeMediator(Func<object, Task<object?>> send)
        {
            _send = send;
        }

        public List<object> Sent { get; } = new();

        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            var response = await Send((object)request, cancellationToken);
            return (TResponse)response!;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            return _send(request);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotImplementedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
            => throw new NotImplementedException();
    }

    // ===== Yardimcilar =====

    private static FeatureDefinition NewFeature(string code, string name)
        => new(code, name);

    private static (FakeAdminAuditSink Sink, FakeAdminAuditContext Context) NewAudit()
        => (new FakeAdminAuditSink(), new FakeAdminAuditContext());

    /// <summary>
    /// FakeMediator'yi donen komut tipine gore sonuc ureten delegate ile kurar.
    /// </summary>
    private static FakeMediator MediatorReturning(Func<object, object> map)
        => new(request => Task.FromResult<object?>(map(request)));

    // ===== Plan listeleme =====

    [Fact]
    public async Task GetAdminSubscriptionPlans_ShouldMatchPlanFeaturesWithDefinitions()
    {
        // Ozellik tanimlari ve iki plan: Baslangic 2 ozellikli, Kurumsal 1 ozellikli.
        var sms = NewFeature("SMS_ONAY", "Sms Onayi");
        var report = NewFeature("RAPOR_LITE", "Rapor Lite");

        var basic = new SubscriptionPlan("Baslangic", 299m, "TRY", EnumBillingPeriod.Monthly);
        var corporate = new SubscriptionPlan("Kurumsal", 999m, "TRY", EnumBillingPeriod.Yearly);

        var plans = new FakeRepository<SubscriptionPlan>(new[] { basic, corporate });
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(basic.Id, sms.Id, true, 100),
            new PlanFeature(basic.Id, report.Id, false, null),
            new PlanFeature(corporate.Id, sms.Id, true, 1000)
        });
        var features = new FakeRepository<FeatureDefinition>(new[] { sms, report });

        var sut = new GetAdminSubscriptionPlansQueryHandler(plans, planFeatures, features);

        var result = await sut.Handle(new GetAdminSubscriptionPlansQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(2, result.Value!.Count);

        // Ad sirasina gore ilk plan Baslangic olmali.
        var first = result.Value[0];
        Assert.Equal(basic.Id, first.Id);
        Assert.Equal("Baslangic", first.Name);
        Assert.Equal(299m, first.Price);
        Assert.Equal(EnumBillingPeriod.Monthly, first.BillingPeriod);
        Assert.True(first.IsActive);

        // Ozellik eslesmesi: kod, aktiflik ve kota dogru karsinmali.
        Assert.Equal(2, first.Features.Count);
        var smsFeature = Assert.Single(first.Features, f => f.FeatureCode == "SMS_ONAY");
        Assert.True(smsFeature.IsEnabled);
        Assert.Equal(100, smsFeature.IncludedQuota);

        var reportFeature = Assert.Single(first.Features, f => f.FeatureCode == "RAPOR_LITE");
        Assert.False(reportFeature.IsEnabled);
        Assert.Null(reportFeature.IncludedQuota);
    }

    // ===== Admin komut sarmalayicilari =====

    [Fact]
    public async Task CreateSubscriptionPlanByAdmin_ShouldSendCreatePlanCommandAndAudit()
    {
        var (audit, auditContext) = NewAudit();
        var planId = Guid.NewGuid();

        var mediator = MediatorReturning(request =>
        {
            Assert.IsType<CreatePlanCommand>(request);
            return Result<Guid>.Success(planId);
        });

        var sut = new CreateSubscriptionPlanByAdminCommandHandler(
            mediator,
            TestCurrentUserService.For(AdminId, "Admin"),
            audit,
            auditContext);

        var command = new CreateSubscriptionPlanByAdminCommand
        {
            Name = "Deneme Plani",
            Price = 149m,
            Currency = "TRY",
            BillingPeriod = EnumBillingPeriod.Monthly,
            Features =
            {
                new AdminPlanFeatureInputDto { FeatureId = Guid.NewGuid(), IsEnabled = true, IncludedQuota = 50 }
            }
        };

        var result = await sut.Handle(command, CancellationToken.None);

        // Sonuc ve iletilen komut.
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(planId, result.Value);

        var sent = Assert.Single(mediator.Sent);
        var inner = Assert.IsType<CreatePlanCommand>(sent);
        Assert.Equal("Deneme Plani", inner.Name);
        Assert.Equal(149m, inner.Price);
        Assert.Equal("TRY", inner.Currency);
        Assert.Equal(EnumBillingPeriod.Monthly, inner.BillingPeriod);
        var feature = Assert.Single(inner.Features);
        Assert.Equal(command.Features[0].FeatureId, feature.FeatureId);
        Assert.True(feature.IsEnabled);
        Assert.Equal(50, feature.IncludedQuota);

        // Denetim kaydi: plan olusturma platform genelidir, businessId bos.
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AdminId, entry.ActorUserId);
        Assert.Equal("Admin", entry.ActorRole);
        Assert.Null(entry.BusinessId);
        Assert.Equal("SubscriptionPlan", entry.EntityType);
        Assert.Equal(planId.ToString(), entry.EntityId);
        Assert.Equal("CreatePlan", entry.Action);
        Assert.Equal("test-correlation", entry.CorrelationId);
        Assert.Equal("127.0.0.1", entry.IpAddress);
    }

    [Fact]
    public async Task AssignSubscriptionByAdmin_ShouldSendAssignCommandAndAuditWithBusiness()
    {
        var (audit, auditContext) = NewAudit();
        var planId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var startAt = NowUtc;
        var endAt = NowUtc.AddMonths(1);

        // Denetim icin mevcut bir aktif abonelik tohumla.
        var existing = new BusinessSubscription(BusinessId, planId, NowUtc.AddMonths(-6));
        var subscriptions = new FakeRepository<BusinessSubscription>(new[] { existing });

        var mediator = MediatorReturning(_ => Result<Guid>.Success(subscriptionId));

        var sut = new AssignSubscriptionByAdminCommandHandler(
            mediator,
            subscriptions,
            TestCurrentUserService.For(AdminId, "Admin"),
            audit,
            auditContext);

        var command = new AssignSubscriptionByAdminCommand
        {
            BusinessId = BusinessId,
            PlanId = planId,
            StartAt = startAt,
            EndAt = endAt
        };

        var result = await sut.Handle(command, CancellationToken.None);

        // Sonuc ve iletilen komut.
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(subscriptionId, result.Value);

        var inner = Assert.IsType<AssignSubscriptionCommand>(Assert.Single(mediator.Sent));
        Assert.Equal(BusinessId, inner.BusinessId);
        Assert.Equal(planId, inner.PlanId);
        Assert.Equal(startAt, inner.StartAt);
        Assert.Equal(endAt, inner.EndAt);

        // Denetim kaydi: isletme kimligi ve eski abonelik ozeti ile.
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AdminId, entry.ActorUserId);
        Assert.Equal("Admin", entry.ActorRole);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal("BusinessSubscription", entry.EntityType);
        Assert.Equal(subscriptionId.ToString(), entry.EntityId);
        Assert.Equal("AssignSubscription", entry.Action);
        Assert.Contains("ActiveCount=1", entry.OldValues);
        Assert.Contains(planId.ToString(), entry.OldValues);
        Assert.Equal("test-correlation", entry.CorrelationId);
        Assert.Equal("127.0.0.1", entry.IpAddress);
    }

    [Fact]
    public async Task GrantFeatureOverrideByAdmin_ShouldSendOverrideCommandAndAudit()
    {
        var (audit, auditContext) = NewAudit();
        var featureId = Guid.NewGuid();
        var overrideId = Guid.NewGuid();

        var mediator = MediatorReturning(_ => Result<Guid>.Success(overrideId));

        var sut = new GrantFeatureOverrideByAdminCommandHandler(
            mediator,
            TestCurrentUserService.For(AdminId, "Admin"),
            audit,
            auditContext);

        var command = new GrantFeatureOverrideByAdminCommand
        {
            BusinessId = BusinessId,
            FeatureId = featureId,
            IsEnabled = true,
            QuotaOverride = 250,
            ValidFrom = NowUtc,
            ValidUntil = NowUtc.AddDays(30),
            Reason = "Kampanya destegi"
        };

        var result = await sut.Handle(command, CancellationToken.None);

        // Sonuc ve iletilen komut.
        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(overrideId, result.Value);

        var inner = Assert.IsType<GrantFeatureOverrideCommand>(Assert.Single(mediator.Sent));
        Assert.Equal(BusinessId, inner.BusinessId);
        Assert.Equal(featureId, inner.FeatureId);
        Assert.True(inner.IsEnabled);
        Assert.Equal(250, inner.QuotaOverride);
        Assert.Equal(NowUtc, inner.ValidFrom);
        Assert.Equal(NowUtc.AddDays(30), inner.ValidUntil);
        Assert.Equal("Kampanya destegi", inner.Reason);

        // Denetim kaydi: isletme + gerekce ile.
        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AdminId, entry.ActorUserId);
        Assert.Equal("Admin", entry.ActorRole);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal("BusinessFeatureOverride", entry.EntityType);
        Assert.Equal(overrideId.ToString(), entry.EntityId);
        Assert.Equal("FeatureOverride", entry.Action);
        Assert.Equal("Kampanya destegi", entry.Reason);
        Assert.Contains(featureId.ToString(), entry.NewValues);
        Assert.Equal("test-correlation", entry.CorrelationId);
        Assert.Equal("127.0.0.1", entry.IpAddress);
    }

    // ===== Isletme aboneligi okuma =====

    [Fact]
    public async Task GetBusinessSubscription_ShouldReturnActiveSubscriptionWithPlanFeatures()
    {
        var sms = NewFeature("WHATSAPP_HATIRLATMA", "Whatsapp Hatirlatma");
        var plan = new SubscriptionPlan("Profesyonel", 499m, "TRY", EnumBillingPeriod.Monthly);

        // Eski abonelik Expired, gecerli olan 10 gun once baslamis.
        var oldSubscription = new BusinessSubscription(BusinessId, plan.Id, NowUtc.AddMonths(-6));
        oldSubscription.MarkExpired();

        var activeSubscription = new BusinessSubscription(BusinessId, plan.Id, NowUtc.AddDays(-10));

        var subscriptions = new FakeRepository<BusinessSubscription>(new[] { oldSubscription, activeSubscription });
        var plans = new FakeRepository<SubscriptionPlan>(new[] { plan });
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(plan.Id, sms.Id, true, 500)
        });
        var features = new FakeRepository<FeatureDefinition>(new[] { sms });

        var sut = new GetBusinessSubscriptionQueryHandler(subscriptions, plans, planFeatures, features);

        var result = await sut.Handle(
            new GetBusinessSubscriptionQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(activeSubscription.Id, result.Value!.SubscriptionId);
        Assert.Equal(plan.Id, result.Value.PlanId);
        Assert.Equal("Profesyonel", result.Value.PlanName);
        Assert.Equal(EnumSubscriptionStatus.Active, result.Value.Status);
        Assert.Null(result.Value.EndAt);

        var feature = Assert.Single(result.Value.PlanFeatures);
        Assert.Equal("WHATSAPP_HATIRLATMA", feature.FeatureCode);
        Assert.True(feature.IsEnabled);
        Assert.Equal(500, feature.IncludedQuota);
    }

    [Fact]
    public async Task GetBusinessSubscription_ShouldReturnNullDataWhenNoSubscription()
    {
        // Aboneligi olmayan isletme: hata degil, bos basari.
        var sut = new GetBusinessSubscriptionQueryHandler(
            new FakeRepository<BusinessSubscription>(),
            new FakeRepository<SubscriptionPlan>(),
            new FakeRepository<PlanFeature>(),
            new FakeRepository<FeatureDefinition>());

        var result = await sut.Handle(
            new GetBusinessSubscriptionQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }
}
