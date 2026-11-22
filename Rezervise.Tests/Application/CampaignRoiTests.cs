using Rezervise.Application.Features.Marketing.Commands.AttributeAppointment;
using Rezervise.Application.Features.Marketing.Commands.CreateCampaign;
using Rezervise.Application.Features.Marketing.Commands.RecordCampaignSpend;
using Rezervise.Application.Features.Marketing.Queries.GetCampaignRoi;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Kampanya olusturma, harcama girisi, randevu atfi (cift atif Conflict)
/// ve ROI ozeti (harcama toplami + atfedilen randevu sayilari).
/// </summary>
public class CampaignRoiTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();

    // ===== Kampanya olusturma =====

    [Fact]
    public async Task CreateCampaign_ShouldStoreActiveCampaign()
    {
        var (sut, campaigns, _) = CreateCampaignSut();

        var result = await sut.Handle(NewCampaignCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(campaigns.Items);
        Assert.True(campaigns.Items[0].IsActive);
        Assert.Equal(EnumCampaignChannel.Instagram, campaigns.Items[0].Channel);
        Assert.Equal(result.Value, campaigns.Items[0].Id);
    }

    [Fact]
    public async Task CreateCampaign_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var (sut, campaigns, _) = CreateCampaignSut(currentUser: TestCurrentUserService.For(Guid.NewGuid()));

        var result = await sut.Handle(NewCampaignCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
        Assert.Empty(campaigns.Items);
    }

    // ===== Harcama girisi =====

    [Fact]
    public async Task RecordSpend_ShouldStoreManualSpend()
    {
        var campaign = NewCampaign();
        var spends = new FakeRepository<CampaignSpend>();
        var sut = new RecordCampaignSpendCommandHandler(
            spends, CampaignRepo(campaign), BusinessRepo(), new FakeUnitOfWork(), CurrentUser());

        var result = await sut.Handle(new RecordCampaignSpendCommand
        {
            CampaignId = campaign.Id,
            SpendDate = new DateTime(2026, 1, 10),
            Amount = 250m
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(spends.Items);
        Assert.Equal(250m, spends.Items[0].Amount);
        Assert.Equal("manual", spends.Items[0].DataSource);
        Assert.Equal("TRY", spends.Items[0].Currency);
    }

    [Fact]
    public async Task RecordSpend_ShouldFailNotFound_WhenCampaignMissing()
    {
        var sut = new RecordCampaignSpendCommandHandler(
            new FakeRepository<CampaignSpend>(),
            new FakeRepository<MarketingCampaign>(),
            BusinessRepo(),
            new FakeUnitOfWork(),
            CurrentUser());

        var result = await sut.Handle(new RecordCampaignSpendCommand
        {
            CampaignId = Guid.NewGuid(),
            SpendDate = new DateTime(2026, 1, 10),
            Amount = 100m
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Randevu atfi =====

    [Fact]
    public async Task Attribute_ShouldSucceedOnce_AndConflictOnSecondAttempt()
    {
        // DuplicateKey korumasi: ayni randevuya ikinci atif Conflict.
        var appointment = NewAppointment(status: EnumAppointmentStatus.Confirmed);
        var attributions = new FakeRepository<AppointmentAttribution>();
        var sut = new AttributeSutBuilder(attributions)
            .WithAppointment(appointment)
            .Build();

        var first = await sut.Handle(NewAttributeCommand(appointment.Id), CancellationToken.None);
        var second = await sut.Handle(NewAttributeCommand(appointment.Id), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(attributions.Items);
    }

    [Fact]
    public async Task Attribute_ShouldFailNotFound_WhenAppointmentBelongsToAnotherBusiness()
    {
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), OtherBusinessId,
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddHours(2));

        var sut = new AttributeSutBuilder(new FakeRepository<AppointmentAttribution>())
            .WithAppointment(appointment)
            .Build();

        var result = await sut.Handle(NewAttributeCommand(appointment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Attribute_ShouldFailUnauthorized_WhenCallerIsNotOwner()
    {
        var appointment = NewAppointment(status: EnumAppointmentStatus.Pending);
        var sut = new AttributeSutBuilder(new FakeRepository<AppointmentAttribution>())
            .WithAppointment(appointment)
            .WithCaller(TestCurrentUserService.For(Guid.NewGuid()))
            .Build();

        var result = await sut.Handle(NewAttributeCommand(appointment.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    // ===== ROI ozeti =====

    [Fact]
    public async Task GetRoi_ShouldSumSpendAndCountAttributions()
    {
        var campaign = NewCampaign();
        var completed = NewAppointment(status: EnumAppointmentStatus.Completed);
        var pending = NewAppointment(status: EnumAppointmentStatus.Pending);

        var spends = new FakeRepository<CampaignSpend>(new[]
        {
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 5), 100m),
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 12), 50m)
        });

        var attributions = new FakeRepository<AppointmentAttribution>(new[]
        {
            new AppointmentAttribution(completed.Id, BusinessId, EnumAttributionModel.LastNonDirectClick, campaignId: campaign.Id),
            new AppointmentAttribution(pending.Id, BusinessId, EnumAttributionModel.LastNonDirectClick, campaignId: campaign.Id)
        });

        var sut = new GetCampaignRoiQueryHandler(
            CampaignRepo(campaign),
            spends,
            attributions,
            new FakeRepository<Appointment>(new[] { completed, pending }),
            BusinessRepo(),
            CurrentUser());

        var result = await sut.Handle(new GetCampaignRoiQuery { CampaignId = campaign.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(150m, result.Value.TotalSpend);
        Assert.Equal(2, result.Value.AttributedAppointments);
        Assert.Equal(1, result.Value.AttributedCompleted);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RoasDesc));
    }

    [Fact]
    public async Task GetRoi_ShouldFilterSpendByDateRange()
    {
        var campaign = NewCampaign();
        var spends = new FakeRepository<CampaignSpend>(new[]
        {
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 1, 5), 100m),
            new CampaignSpend(BusinessId, campaign.Id, new DateTime(2026, 2, 20), 50m)
        });

        var sut = new GetCampaignRoiQueryHandler(
            CampaignRepo(campaign),
            spends,
            new FakeRepository<AppointmentAttribution>(),
            new FakeRepository<Appointment>(),
            BusinessRepo(),
            CurrentUser());

        var result = await sut.Handle(new GetCampaignRoiQuery
        {
            CampaignId = campaign.Id,
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 1, 31)
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.TotalSpend);
        Assert.Equal(0, result.Value.AttributedAppointments);
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

    private static FakeRepository<MarketingCampaign> CampaignRepo(MarketingCampaign? campaign = null)
        => campaign is null ? new FakeRepository<MarketingCampaign>() : new(new[] { campaign });

    private static MarketingCampaign NewCampaign()
    {
        var campaign = new MarketingCampaign(
            BusinessId, "Instagram Kampanyasi", EnumCampaignChannel.Instagram);
        campaign.ClearDomainEvents();
        return campaign;
    }

    private static (CreateCampaignCommandHandler Sut, FakeRepository<MarketingCampaign> Campaigns, FakeUnitOfWork UnitOfWork)
        CreateCampaignSut(TestCurrentUserService? currentUser = null)
    {
        var campaigns = new FakeRepository<MarketingCampaign>();
        var unitOfWork = new FakeUnitOfWork();

        return (
            new CreateCampaignCommandHandler(campaigns, BusinessRepo(), unitOfWork, currentUser ?? CurrentUser()),
            campaigns,
            unitOfWork);
    }

    private static CreateCampaignCommand NewCampaignCommand() => new()
    {
        BusinessId = BusinessId,
        Name = "Instagram Kampanyasi",
        Channel = EnumCampaignChannel.Instagram
    };

    private static Appointment NewAppointment(EnumAppointmentStatus status)
        => TestData.NewAppointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(2),
            status);

    private static AttributeAppointmentCommand NewAttributeCommand(Guid appointmentId) => new()
    {
        AppointmentId = appointmentId,
        BusinessId = BusinessId,
        AttributionModel = EnumAttributionModel.LastNonDirectClick
    };

    /// <summary>Atif handler'inin bagimliliklarini adim adim kurar.</summary>
    private sealed class AttributeSutBuilder
    {
        private readonly FakeRepository<AppointmentAttribution> _attributions;
        private FakeRepository<Appointment> _appointments = new();
        private TestCurrentUserService _caller = TestCurrentUserService.For(OwnerId);

        public AttributeSutBuilder(FakeRepository<AppointmentAttribution> attributions)
        {
            _attributions = attributions;
        }

        public AttributeSutBuilder WithAppointment(Appointment appointment)
        {
            _appointments = new FakeRepository<Appointment>(new[] { appointment });
            return this;
        }

        public AttributeSutBuilder WithCaller(TestCurrentUserService caller)
        {
            _caller = caller;
            return this;
        }

        public AttributeAppointmentCommandHandler Build()
            => new(
                _attributions,
                _appointments,
                new FakeRepository<MarketingCampaign>(),
                new FakeRepository<TrackingLink>(),
                BusinessRepo(),
                new FakeUnitOfWork(),
                _caller);
    }
}
