using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Features.Subscriptions.Commands.GrantFeatureOverride;
using AppointMan.Domain.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Infrastructure.Services;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// FeatureAccessService'in gercek implementasyonunun FakeRepository'ler ve
/// StubDateTimeService ile izole testleri (dokuman Modul 14.4).
/// </summary>
public class FeatureAccessServiceTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private readonly FeatureDefinition _whatsAppDefinition = new(FeatureCodes.WhatsAppReminder, "WhatsApp hatirlatma");
    private readonly FeatureDefinition _basicDefinition = new(FeatureCodes.AppointmentBasic, "Randevu temel");

    private static StubDateTimeService NewClock() => new(Now);

    private static SubscriptionPlan NewPlan()
        => new("Test Plani", 500m, "TRY", EnumBillingPeriod.Monthly);

    private static BusinessSubscription NewActiveSubscription(Guid businessId, Guid planId)
        => new(businessId, planId, Now.AddDays(-30));

    private FeatureAccessService NewSut(
        FakeRepository<BusinessFeatureOverride>? overrides = null,
        FakeRepository<BusinessSubscription>? subscriptions = null,
        FakeRepository<PlanFeature>? planFeatures = null,
        FakeRepository<FeatureDefinition>? definitions = null,
        FakeRepository<FeatureUsage>? usages = null,
        StubDateTimeService? clock = null)
    {
        return new FeatureAccessService(
            overrides ?? new FakeRepository<BusinessFeatureOverride>(),
            subscriptions ?? new FakeRepository<BusinessSubscription>(),
            planFeatures ?? new FakeRepository<PlanFeature>(),
            definitions ?? new FakeRepository<FeatureDefinition>(new[] { _whatsAppDefinition, _basicDefinition }),
            usages ?? new FakeRepository<FeatureUsage>(),
            clock ?? NewClock());
    }

    [Fact]
    public async Task IsEnabled_AktifOverride_PlandakiKapaliOzelligiEzer()
    {
        // Planda kapali; aktif override acik.
        var plan = NewPlan();
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(plan.Id, _whatsAppDefinition.Id, isEnabled: false, includedQuota: null)
        });
        var subscriptions = new FakeRepository<BusinessSubscription>(new[]
        {
            NewActiveSubscription(BusinessId, plan.Id)
        });
        var overrides = new FakeRepository<BusinessFeatureOverride>(new[]
        {
            new BusinessFeatureOverride(
                BusinessId, _whatsAppDefinition.Id,
                isEnabled: true, quotaOverride: null,
                validFrom: Now.AddDays(-1), validUntil: null,
                reason: "Kampanya", grantedBy: Guid.NewGuid())
        });

        var sut = NewSut(overrides, subscriptions, planFeatures);

        var result = await sut.IsEnabledAsync(BusinessId, FeatureCodes.WhatsAppReminder);

        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabled_SuresiDolmusOverride_Yoksayilir()
    {
        // Planda acik; override kapali ama suresi dolmus -> override yok sayilir.
        var plan = NewPlan();
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(plan.Id, _whatsAppDefinition.Id, isEnabled: true, includedQuota: null)
        });
        var subscriptions = new FakeRepository<BusinessSubscription>(new[]
        {
            NewActiveSubscription(BusinessId, plan.Id)
        });
        var overrides = new FakeRepository<BusinessFeatureOverride>(new[]
        {
            new BusinessFeatureOverride(
                BusinessId, _whatsAppDefinition.Id,
                isEnabled: false, quotaOverride: null,
                validFrom: Now.AddDays(-30), validUntil: Now.AddDays(-1),
                reason: null, grantedBy: Guid.NewGuid())
        });

        var sut = NewSut(overrides, subscriptions, planFeatures);

        var result = await sut.IsEnabledAsync(BusinessId, FeatureCodes.WhatsAppReminder);

        Assert.True(result);
    }

    [Fact]
    public async Task IsEnabled_OverrideKapali_IseErisimKapali()
    {
        // Planda acik; aktif override kapali -> override kazanir.
        var plan = NewPlan();
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(plan.Id, _whatsAppDefinition.Id, isEnabled: true, includedQuota: null)
        });
        var subscriptions = new FakeRepository<BusinessSubscription>(new[]
        {
            NewActiveSubscription(BusinessId, plan.Id)
        });
        var overrides = new FakeRepository<BusinessFeatureOverride>(new[]
        {
            new BusinessFeatureOverride(
                BusinessId, _whatsAppDefinition.Id,
                isEnabled: false, quotaOverride: null,
                validFrom: Now.AddDays(-1), validUntil: null,
                reason: "Iade sonrasi kapatma", grantedBy: Guid.NewGuid())
        });

        var sut = NewSut(overrides, subscriptions, planFeatures);

        var result = await sut.IsEnabledAsync(BusinessId, FeatureCodes.WhatsAppReminder);

        Assert.False(result);
    }

    [Fact]
    public async Task TryReserve_PlaniKotasi500_Kullanim499_IleBirAdimTrue_IkiAdimFalse()
    {
        var plan = NewPlan();
        var planFeatures = new FakeRepository<PlanFeature>(new[]
        {
            new PlanFeature(plan.Id, _whatsAppDefinition.Id, isEnabled: true, includedQuota: 500)
        });
        var subscriptions = new FakeRepository<BusinessSubscription>(new[]
        {
            NewActiveSubscription(BusinessId, plan.Id)
        });
        // Donem, StubDateTimeService'in UtcNow degerinden turetilir: "2026-11".
        var usages = new FakeRepository<FeatureUsage>(new[]
        {
            new FeatureUsage(BusinessId, _whatsAppDefinition.Id, "2026-11", usedQuantity: 499)
        });

        var sut = NewSut(subscriptions: subscriptions, planFeatures: planFeatures, usages: usages);

        var firstReserve = await sut.TryReserveQuotaAsync(BusinessId, FeatureCodes.WhatsAppReminder, 1);
        var secondReserve = await sut.TryReserveQuotaAsync(BusinessId, FeatureCodes.WhatsAppReminder, 2);

        Assert.True(firstReserve);
        Assert.False(secondReserve);

        // Basarili rezervasyon sayaci 500'e tamamladi; asim yazmadi.
        var usage = Assert.Single(usages.Items);
        Assert.Equal(500, usage.UsedQuantity);
    }

    [Fact]
    public async Task IsEnabled_AbonelikVeOverrideYok_TemelOzellikTrue_DigerleriFalse()
    {
        var sut = NewSut();

        var basic = await sut.IsEnabledAsync(BusinessId, FeatureCodes.AppointmentBasic);
        var whatsApp = await sut.IsEnabledAsync(BusinessId, FeatureCodes.WhatsAppReminder);

        Assert.True(basic);
        Assert.False(whatsApp);
    }

    [Fact]
    public async Task TryReserve_AbonelikYokTemelOzellik_SinirsizRezervasyon()
    {
        var sut = NewSut();

        var result = await sut.TryReserveQuotaAsync(BusinessId, FeatureCodes.AppointmentBasic, 1);

        Assert.True(result);
    }

    // ============= GrantFeatureOverride yetki testi =============

    [Fact]
    public async Task GrantFeatureOverride_AdminOlmayanCaller_ForbiddenDoner()
    {
        var currentUser = TestCurrentUserService.For(Guid.NewGuid());
        var handler = new GrantFeatureOverrideCommandHandler(
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeRepository<FeatureDefinition>(new[] { _whatsAppDefinition }),
            new FakeRepository<BusinessFeatureOverride>(),
            new FakeUnitOfWork(),
            currentUser,
            new StubWorkflowAuditSink());

        var command = new GrantFeatureOverrideCommand
        {
            BusinessId = BusinessId,
            FeatureId = _whatsAppDefinition.Id,
            IsEnabled = true,
            ValidFrom = Now,
            ValidUntil = null,
            Reason = "Test"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
    }

    /// <summary>Testler icin kayitlari bellekte tutan denetim havuzu.</summary>
    private sealed class StubWorkflowAuditSink : IWorkflowAuditSink
    {
        public List<(string EventType, Guid? AggregateId)> Records { get; } = new();

        public void Record(string eventType, Guid eventId, Guid? aggregateId, string? details)
        {
            Records.Add((eventType, aggregateId));
        }
    }
}
