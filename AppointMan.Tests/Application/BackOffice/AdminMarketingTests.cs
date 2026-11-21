using AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminCampaignPerformance;
using AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminCampaigns;
using AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminRecoveredSlots;
using AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts;
using AppointMan.Application.Features.Admin.Marketing.Queries.GetAdminWaitlistOverview;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Backoffice pazarlama raporlari (dokuman Modul 15): kampanya toplamlari,
/// performans kirilimi, bekleme listesi ozeti, geri cagirma denemeleri ve
/// kurtarilan kapasite raporu.
/// </summary>
public class AdminMarketingTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();

    // ===== Kampanya listesi =====

    [Fact]
    public async Task GetCampaigns_ShouldAggregateSpendAndAttributions()
    {
        var campaign = NewCampaign("Instagram Kampanyasi", EnumCampaignChannel.Instagram);
        var otherCampaign = NewCampaign("Google Kampanyasi", EnumCampaignChannel.GoogleAds);

        var spends = new FakeRepository<CampaignSpend>(new[]
        {
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 5), 500m),
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 12), 300m)
        });

        var attributions = new FakeRepository<AppointmentAttribution>(new[]
        {
            NewAttribution(campaign.Id),
            NewAttribution(campaign.Id)
        });

        var sut = new GetAdminCampaignsQueryHandler(
            CampaignRepo(campaign, otherCampaign), spends, attributions);

        var result = await sut.Handle(new GetAdminCampaignsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);

        var dto = result.Value.Items.Single(c => c.Id == campaign.Id);
        Assert.Equal(800m, dto.TotalSpend);
        Assert.Equal(2, dto.AttributedAppointments);

        // Harcama ve atif girilmeyen kampanya sifir toplamla listelenir.
        var otherDto = result.Value.Items.Single(c => c.Id == otherCampaign.Id);
        Assert.Equal(0m, otherDto.TotalSpend);
        Assert.Equal(0, otherDto.AttributedAppointments);
    }

    [Fact]
    public async Task GetCampaigns_ShouldFilterByBusiness()
    {
        var own = NewCampaign("Kampanya 1", EnumCampaignChannel.Instagram);
        own.Deactivate();

        var other = new MarketingCampaign(
            OtherBusinessId, "Baska isletme", EnumCampaignChannel.Organic);

        var sut = new GetAdminCampaignsQueryHandler(
            CampaignRepo(own, other),
            new FakeRepository<CampaignSpend>(),
            new FakeRepository<AppointmentAttribution>());

        var result = await sut.Handle(
            new GetAdminCampaignsQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!.Items);
        Assert.Equal(own.Id, dto.Id);
        Assert.Equal(BusinessId, dto.BusinessId);
    }

    // ===== Kampanya performansi =====

    [Fact]
    public async Task GetCampaignPerformance_ShouldCountCompletedAttributedAppointments()
    {
        var campaign = NewCampaign("Instagram Kampanyasi", EnumCampaignChannel.Instagram);

        var completed = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), BusinessId,
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(-2),
            EnumAppointmentStatus.Completed);

        var pending = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), BusinessId,
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(2),
            EnumAppointmentStatus.Pending);

        var spends = new FakeRepository<CampaignSpend>(new[]
        {
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 5), 200m)
        });

        var attributions = new FakeRepository<AppointmentAttribution>(new[]
        {
            NewAttribution(campaign.Id, completed.Id),
            NewAttribution(campaign.Id, pending.Id)
        });

        var sut = new GetAdminCampaignPerformanceQueryHandler(
            CampaignRepo(campaign),
            spends,
            attributions,
            new FakeRepository<Appointment>(new[] { completed, pending }));

        var result = await sut.Handle(
            new GetAdminCampaignPerformanceQuery { CampaignId = campaign.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(campaign.Name, result.Value!.CampaignName);
        Assert.Equal(200m, result.Value.TotalSpend);
        Assert.Equal(2, result.Value.AttributedAppointments);
        Assert.Equal(1, result.Value.AttributedCompleted);

        // ROAS aciklamasi GetCampaignRoi deseniyle uyumlu metindir.
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RoasDesc));
    }

    [Fact]
    public async Task GetCampaignPerformance_ShouldFailNotFound_WhenCampaignMissing()
    {
        var sut = new GetAdminCampaignPerformanceQueryHandler(
            new FakeRepository<MarketingCampaign>(),
            new FakeRepository<CampaignSpend>(),
            new FakeRepository<AppointmentAttribution>(),
            new FakeRepository<Appointment>());

        var result = await sut.Handle(
            new GetAdminCampaignPerformanceQuery { CampaignId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Bekleme listesi ozeti =====

    [Fact]
    public async Task GetWaitlistOverview_ShouldCountStatuses()
    {
        var now = DateTime.UtcNow;

        // 3 aktif kayit + 1 tamamlanmis kayit: yalnizca aktifler sayilir.
        var entries = new List<WaitlistEntry>();
        for (var i = 0; i < 3; i++)
            entries.Add(NewWaitlistEntry(BusinessId));

        var fulfilled = NewWaitlistEntry(BusinessId);
        fulfilled.Fulfill();
        entries.Add(fulfilled);

        // Baska isletmenin aktif kaydi: isletme filtresinde elenmeli.
        entries.Add(NewWaitlistEntry(OtherBusinessId));

        var acceptedOffer = NewOffer(entries[0].Id, BusinessId, now);
        acceptedOffer.Accept(now);

        var expiredOffer = NewOffer(entries[1].Id, BusinessId, now);
        expiredOffer.Expire(now);

        var slots = new List<RecoveredSlot>();
        for (var i = 0; i < 2; i++)
        {
            var slot = new RecoveredSlot(BusinessId, Guid.NewGuid(), now.AddDays(-i - 1));
            slot.Rebook(Guid.NewGuid(), now);
            slots.Add(slot);
        }

        var lostSlot = new RecoveredSlot(BusinessId, Guid.NewGuid(), now.AddDays(-3));
        lostSlot.MarkLost(now);
        slots.Add(lostSlot);

        var sut = new GetAdminWaitlistOverviewQueryHandler(
            new FakeRepository<WaitlistEntry>(entries),
            new FakeRepository<WaitlistOffer>(new[] { acceptedOffer, expiredOffer }),
            new FakeRepository<RecoveredSlot>(slots));

        var filtered = await sut.Handle(
            new GetAdminWaitlistOverviewQuery { BusinessId = BusinessId },
            CancellationToken.None);

        Assert.True(filtered.IsSuccess);
        Assert.Equal(BusinessId, filtered.Value!.BusinessId);
        Assert.Equal(3, filtered.Value.ActiveEntries);
        Assert.Equal(0, filtered.Value.PendingOffers);
        Assert.Equal(1, filtered.Value.AcceptedOffers);
        Assert.Equal(1, filtered.Value.ExpiredOffers);
        Assert.Equal(2, filtered.Value.RebookedSlots);
        Assert.Equal(1, filtered.Value.LostSlots);

        // Filtresiz rapor diger isletmenin kaydini da sayar.
        var overall = await sut.Handle(
            new GetAdminWaitlistOverviewQuery(), CancellationToken.None);

        Assert.True(overall.IsSuccess);
        Assert.Equal(4, overall.Value!.ActiveEntries);
    }

    // ===== Geri cagirma denemeleri =====

    [Fact]
    public async Task GetReengagementAttempts_ShouldFilterByStatusAndDate()
    {
        var now = DateTime.UtcNow;

        var oldNotified = NewAttempt(now.AddDays(-10));
        oldNotified.MarkNotified();

        var recentNotified = NewAttempt(now.AddHours(-1));
        recentNotified.MarkNotified();

        var converted = NewAttempt(now.AddHours(-2));
        converted.MarkNotified();
        converted.MarkConverted(Guid.NewGuid());

        var sut = new GetAdminReengagementAttemptsQueryHandler(
            new FakeRepository<ReengagementAttempt>(new[] { oldNotified, recentNotified, converted }));

        var byStatus = await sut.Handle(
            new GetAdminReengagementAttemptsQuery { Status = EnumReengagementStatus.Notified },
            CancellationToken.None);

        Assert.True(byStatus.IsSuccess);
        Assert.Equal(2, byStatus.Value!.Items.Count);
        Assert.All(byStatus.Value.Items, a => Assert.Equal(EnumReengagementStatus.Notified, a.Status));

        var byStatusAndDate = await sut.Handle(
            new GetAdminReengagementAttemptsQuery
            {
                Status = EnumReengagementStatus.Notified,
                FromDate = now.AddDays(-1)
            },
            CancellationToken.None);

        Assert.True(byStatusAndDate.IsSuccess);
        var item = Assert.Single(byStatusAndDate.Value!.Items);
        Assert.Equal(recentNotified.Id, item.Id);
    }

    // ===== Kurtarilan kapasite raporu =====

    [Fact]
    public async Task GetRecoveredSlots_ShouldListSlotsAndSummarizeRebooked()
    {
        var now = DateTime.UtcNow;

        var rebookedOld = new RecoveredSlot(BusinessId, Guid.NewGuid(), now.AddDays(-40));
        rebookedOld.Rebook(Guid.NewGuid(), now.AddDays(-39));

        var rebookedRecent = new RecoveredSlot(BusinessId, Guid.NewGuid(), now.AddDays(-1));
        rebookedRecent.Rebook(Guid.NewGuid(), now);

        var lostRecent = new RecoveredSlot(BusinessId, Guid.NewGuid(), now.AddDays(-2));
        lostRecent.MarkLost(now);

        var openRecent = new RecoveredSlot(OtherBusinessId, Guid.NewGuid(), now.AddHours(-3));

        var sut = new GetAdminRecoveredSlotsQueryHandler(
            new FakeRepository<RecoveredSlot>(new[] { rebookedOld, rebookedRecent, lostRecent, openRecent }));

        var overall = await sut.Handle(new GetAdminRecoveredSlotsQuery(), CancellationToken.None);

        Assert.True(overall.IsSuccess);
        Assert.Equal(4, overall.Value!.Items.Count);
        Assert.Equal(2, overall.Value.TotalRebooked);

        // Tarih filtresi pencere disindaki kaydi eler; ozet yalniz filtre uzerinden hesaplanir.
        var filtered = await sut.Handle(
            new GetAdminRecoveredSlotsQuery { FromDate = now.AddDays(-30) },
            CancellationToken.None);

        Assert.True(filtered.IsSuccess);
        Assert.Equal(3, filtered.Value!.Items.Count);
        // Filtre penceresi rebookedOld'u eler; ozet yalniz filtre uzerinden hesaplanir.
        Assert.Equal(1, filtered.Value.TotalRebooked);

        // Liste acilis zamanina gore tersten siralanir:
        // openRecent (-3h) > rebookedRecent (-1g) > lostRecent (-2g).
        Assert.Equal(openRecent.Id, filtered.Value.Items[0].Id);
        Assert.Equal(rebookedRecent.Id, filtered.Value.Items[1].Id);
        Assert.Equal(lostRecent.Id, filtered.Value.Items[2].Id);
    }

    // ===== Yardimcilar =====

    private static FakeRepository<MarketingCampaign> CampaignRepo(params MarketingCampaign[] campaigns)
        => new(campaigns);

    private static MarketingCampaign NewCampaign(string name, EnumCampaignChannel channel)
    {
        var campaign = new MarketingCampaign(BusinessId, name, channel);
        campaign.ClearDomainEvents();
        return campaign;
    }

    private static AppointmentAttribution NewAttribution(Guid campaignId, Guid? appointmentId = null)
        => new(
            appointmentId ?? Guid.NewGuid(),
            BusinessId,
            EnumAttributionModel.LastNonDirectClick,
            campaignId: campaignId);

    private static WaitlistEntry NewWaitlistEntry(Guid businessId)
    {
        var entry = new WaitlistEntry(
            businessId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Date.AddDays(-1));
        entry.ClearDomainEvents();
        return entry;
    }

    private static WaitlistOffer NewOffer(Guid entryId, Guid businessId, DateTime now)
        => new(entryId, businessId, now.AddHours(2), now.AddHours(1), now);

    private static ReengagementAttempt NewAttempt(DateTime triggeredAt)
    {
        var attempt = new ReengagementAttempt(
            BusinessId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), triggeredAt);
        attempt.ClearDomainEvents();
        return attempt;
    }
}
