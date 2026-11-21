using AppointMan.Application.Features.Admin.Reports.Queries.GetFeatureAdoption;
using AppointMan.Application.Features.Admin.Reports.Queries.GetPlatformOverview;
using AppointMan.Application.Features.Admin.Reports.Queries.GetSubscriptionRevenue;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Platform raporlama handler testleri (dokuman Modul 18). Sayim ve oran
/// hesaplari hazir veri uzerinden dogrulanir.
/// </summary>
public class AdminReportTests
{
    // ===== GetPlatformOverview =====

    [Fact]
    public async Task PlatformOverview_ShouldCountBusinessesAndAppointments()
    {
        // 1 aktif + 1 askida isletme; CreatedAt simdiki zaman (son 30 gun).
        var active = TestData.NewBusiness(Guid.NewGuid());
        var suspended = TestData.NewBusiness(Guid.NewGuid());
        suspended.Suspend("tester", "test");

        // 3 randevu: 1 Completed, 1 Cancelled, 1 NoShow.
        var completed = NewAppointment(active.Id, EnumAppointmentStatus.Completed);
        var cancelled = NewAppointment(active.Id, EnumAppointmentStatus.Cancelled);
        var noShow = NewAppointment(suspended.Id, EnumAppointmentStatus.NoShow);

        var handler = new GetPlatformOverviewQueryHandler(
            new FakeRepository<Business>(new[] { active, suspended }),
            new FakeRepository<Appointment>(new[] { completed, cancelled, noShow }));

        var result = await handler.Handle(new GetPlatformOverviewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;

        Assert.Equal(2, dto.TotalBusinesses);
        Assert.Equal(1, dto.ActiveBusinesses);
        Assert.Equal(1, dto.SuspendedBusinesses);
        Assert.Equal(2, dto.NewRegistrations30Days);

        Assert.Equal(3, dto.TotalAppointments);
        Assert.Equal(1, dto.CompletedAppointments);

        // 3 randevunun 1'i iptal, 1'i no-show: yuzde 33.33.
        Assert.Equal(33.33m, dto.CancellationRate);
        Assert.Equal(33.33m, dto.NoShowRate);
    }

    [Fact]
    public async Task PlatformOverview_ShouldReturnZeroRates_WhenNoAppointments()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());

        var handler = new GetPlatformOverviewQueryHandler(
            new FakeRepository<Business>(new[] { business }),
            new FakeRepository<Appointment>());

        var result = await handler.Handle(new GetPlatformOverviewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalAppointments);
        Assert.Equal(0m, result.Value.CancellationRate);
        Assert.Equal(0m, result.Value.NoShowRate);
    }

    // ===== GetSubscriptionRevenue =====

    [Fact]
    public async Task SubscriptionRevenue_ShouldComputeMrrAndPlanDistribution()
    {
        // 2 aktif Monthly 500 + 1 aktif Yearly 1200.
        // MRR = 500*2 + 1200/12 = 1100.
        var monthlyPlan = new SubscriptionPlan(Guid.NewGuid(), "Temel Aylik", 500m, "TRY", EnumBillingPeriod.Monthly);
        var yearlyPlan = new SubscriptionPlan(Guid.NewGuid(), "Kurumsal Yillik", 1200m, "TRY", EnumBillingPeriod.Yearly);

        var startAt = DateTime.UtcNow.AddDays(-10);

        var subscriptions = new[]
        {
            new BusinessSubscription(Guid.NewGuid(), monthlyPlan.Id, startAt),
            new BusinessSubscription(Guid.NewGuid(), monthlyPlan.Id, startAt),
            new BusinessSubscription(Guid.NewGuid(), yearlyPlan.Id, startAt),
            // Suresi dolmus abonelik aktif sayilmaz.
            new BusinessSubscription(Guid.NewGuid(), monthlyPlan.Id, startAt, DateTime.UtcNow.AddDays(-1))
        };

        var handler = new GetSubscriptionRevenueQueryHandler(
            new FakeRepository<BusinessSubscription>(subscriptions),
            new FakeRepository<SubscriptionPlan>(new[] { monthlyPlan, yearlyPlan }));

        var result = await handler.Handle(new GetSubscriptionRevenueQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;

        Assert.Equal(3, dto.ActiveSubscriptionCount);
        Assert.Equal(1100m, dto.Mrr);
        Assert.Equal("TRY", dto.Currency);

        var monthly = Assert.Single(dto.Plans, p => p.PlanId == monthlyPlan.Id);
        Assert.Equal(2, monthly.BusinessCount);

        var yearly = Assert.Single(dto.Plans, p => p.PlanId == yearlyPlan.Id);
        Assert.Equal(1, yearly.BusinessCount);
    }

    [Fact]
    public async Task SubscriptionRevenue_ShouldExcludeCancelledSubscriptions()
    {
        var plan = new SubscriptionPlan(Guid.NewGuid(), "Temel Aylik", 500m, "TRY", EnumBillingPeriod.Monthly);

        var cancelled = new BusinessSubscription(Guid.NewGuid(), plan.Id, DateTime.UtcNow.AddDays(-10));
        cancelled.MarkCancelled();

        var handler = new GetSubscriptionRevenueQueryHandler(
            new FakeRepository<BusinessSubscription>(new[] { cancelled }),
            new FakeRepository<SubscriptionPlan>(new[] { plan }));

        var result = await handler.Handle(new GetSubscriptionRevenueQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.ActiveSubscriptionCount);
        Assert.Equal(0m, result.Value.Mrr);
        Assert.Empty(result.Value.Plans);
    }

    // ===== GetFeatureAdoption =====

    [Fact]
    public async Task FeatureAdoption_ShouldCountBusinessesPerFeature()
    {
        var reminder = new FeatureDefinition(Guid.NewGuid(), "WHATSAPP_REMINDER", "WhatsApp Hatirlatma");
        var campaign = new FeatureDefinition(Guid.NewGuid(), "CAMPAIGN_ROI", "Kampanya ROI");

        var overrides = new[]
        {
            // Ayni ozellik 2 ayri isletmede acik.
            NewOverride(Guid.NewGuid(), reminder.Id, isEnabled: true),
            NewOverride(Guid.NewGuid(), reminder.Id, isEnabled: true),
            // Baska ozellik ayri satirda sayilir.
            NewOverride(Guid.NewGuid(), campaign.Id, isEnabled: true),
            // Kapali override benimseme sayisina girmez.
            NewOverride(Guid.NewGuid(), reminder.Id, isEnabled: false)
        };

        var handler = new GetFeatureAdoptionQueryHandler(
            new FakeRepository<BusinessFeatureOverride>(overrides),
            new FakeRepository<FeatureDefinition>(new[] { reminder, campaign }));

        var result = await handler.Handle(new GetFeatureAdoptionQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var adoption = result.Value!;

        var reminderRow = Assert.Single(adoption, a => a.FeatureCode == "WHATSAPP_REMINDER");
        Assert.Equal(2, reminderRow.BusinessCount);

        var campaignRow = Assert.Single(adoption, a => a.FeatureCode == "CAMPAIGN_ROI");
        Assert.Equal(1, campaignRow.BusinessCount);
    }

    // ===== Yardimcilar =====

    private static Appointment NewAppointment(Guid businessId, EnumAppointmentStatus status)
        => TestData.NewAppointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            businessId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-1),
            status);

    private static BusinessFeatureOverride NewOverride(Guid businessId, Guid featureId, bool isEnabled)
        => new(
            businessId,
            featureId,
            isEnabled,
            quotaOverride: null,
            validFrom: DateTime.UtcNow.AddDays(-1),
            validUntil: null,
            reason: "test",
            grantedBy: Guid.NewGuid());
}
