using AppointMan.Application.Features.Consents.Commands.SetCommunicationConsent;
using AppointMan.Application.Features.Consents.Queries.CanContact;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Iletisim izinleri akisinin izole testleri (dokuman Modul 16).
/// </summary>
public class ConsentTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static SetCommunicationConsentCommand NewSetCommand(
        EnumConsentChannel channel = EnumConsentChannel.Sms,
        EnumConsentPurpose purpose = EnumConsentPurpose.Marketing,
        bool isAllowed = false) => new()
    {
        BusinessId = BusinessId,
        CustomerId = CustomerId,
        Channel = channel,
        Purpose = purpose,
        IsAllowed = isAllowed,
        Source = "api"
    };

    private static SetCommunicationConsentCommandHandler NewSetHandler(
        TestCurrentUserService currentUser,
        FakeRepository<CustomerCommunicationPreference>? preferences = null,
        FakeRepository<ConsentHistory>? histories = null,
        FakeRepository<CommunicationSuppression>? suppressions = null)
    {
        return new SetCommunicationConsentCommandHandler(
            NewBusinessRepository(),
            preferences ?? new FakeRepository<CustomerCommunicationPreference>(),
            histories ?? new FakeRepository<ConsentHistory>(),
            suppressions ?? new FakeRepository<CommunicationSuppression>(),
            new FakeUnitOfWork(),
            currentUser);
    }

    private static CanContactQueryHandler NewCanContactHandler(
        TestCurrentUserService currentUser,
        FakeRepository<CustomerCommunicationPreference>? preferences = null,
        FakeRepository<CommunicationSuppression>? suppressions = null)
    {
        return new CanContactQueryHandler(
            NewBusinessRepository(),
            preferences ?? new FakeRepository<CustomerCommunicationPreference>(),
            suppressions ?? new FakeRepository<CommunicationSuppression>(),
            currentUser);
    }

    private static FakeRepository<AppointMan.Domain.Entities.Business> NewBusinessRepository()
        => new(new[] { TestData.NewBusiness(OwnerId, BusinessId) });

    [Fact]
    public async Task SetConsent_PazarlamaIzniGeriCekilirse_SuppressionOlusur()
    {
        var preferences = new FakeRepository<CustomerCommunicationPreference>();
        var histories = new FakeRepository<ConsentHistory>();
        var suppressions = new FakeRepository<CommunicationSuppression>();

        var handler = NewSetHandler(TestCurrentUserService.For(OwnerId), preferences, histories, suppressions);

        var result = await handler.Handle(
            NewSetCommand(EnumConsentChannel.Sms, EnumConsentPurpose.Marketing, isAllowed: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        // Suppression olusmali ve gecmis kaydi Withdrawn olarak dusmeli.
        var suppression = Assert.Single(suppressions.Items);
        Assert.Equal(BusinessId, suppression.BusinessId);
        Assert.Equal(CustomerId, suppression.CustomerId);
        Assert.Equal(EnumConsentChannel.Sms, suppression.Channel);

        var history = Assert.Single(histories.Items);
        Assert.Equal(EnumConsentAction.Withdrawn, history.Action);
    }

    [Fact]
    public async Task CanContact_PazarlamaIzniGeriCekildiyse_FalseDoner()
    {
        var suppressions = new FakeRepository<CommunicationSuppression>(new[]
        {
            new CommunicationSuppression(BusinessId, CustomerId, EnumConsentChannel.Sms, "Pazarlama izni geri cekildi.")
        });

        var handler = NewCanContactHandler(TestCurrentUserService.For(OwnerId), suppressions: suppressions);

        var result = await handler.Handle(
            new CanContactQuery
            {
                BusinessId = BusinessId,
                CustomerId = CustomerId,
                Channel = EnumConsentChannel.Sms,
                Purpose = EnumConsentPurpose.Marketing
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task CanContact_TercihYok_IseIslemselVarsayilanTrue()
    {
        var handler = NewCanContactHandler(TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(
            new CanContactQuery
            {
                BusinessId = BusinessId,
                CustomerId = CustomerId,
                Channel = EnumConsentChannel.Sms,
                Purpose = EnumConsentPurpose.Transactional
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task CanContact_TercihYok_IsePazarlamaVarsayilanFalse()
    {
        var handler = NewCanContactHandler(TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(
            new CanContactQuery
            {
                BusinessId = BusinessId,
                CustomerId = CustomerId,
                Channel = EnumConsentChannel.Sms,
                Purpose = EnumConsentPurpose.Marketing
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task SetConsent_IzinVerilince_MevcutSuppressionSilinir()
    {
        var preferences = new FakeRepository<CustomerCommunicationPreference>();
        var suppressions = new FakeRepository<CommunicationSuppression>(new[]
        {
            new CommunicationSuppression(BusinessId, CustomerId, EnumConsentChannel.Sms, "Onceki cekilis")
        });

        var handler = NewSetHandler(TestCurrentUserService.For(OwnerId), preferences, suppressions: suppressions);

        var result = await handler.Handle(
            NewSetCommand(EnumConsentChannel.Sms, EnumConsentPurpose.Marketing, isAllowed: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(suppressions.Items);

        // Suppression kalkti ve tercih acik: artik iletisime gecilebilir.
        var canContactHandler = NewCanContactHandler(
            TestCurrentUserService.For(OwnerId), preferences: preferences, suppressions: suppressions);

        var canContact = await canContactHandler.Handle(
            new CanContactQuery
            {
                BusinessId = BusinessId,
                CustomerId = CustomerId,
                Channel = EnumConsentChannel.Sms,
                Purpose = EnumConsentPurpose.Marketing
            },
            CancellationToken.None);

        Assert.True(canContact.IsSuccess);
        Assert.True(canContact.Value);
    }

    [Fact]
    public async Task SetConsent_IslemselIzinGeriCekilirse_SuppressionOlusmaz()
    {
        var suppressions = new FakeRepository<CommunicationSuppression>();

        var handler = NewSetHandler(TestCurrentUserService.For(OwnerId), suppressions: suppressions);

        var result = await handler.Handle(
            NewSetCommand(EnumConsentChannel.Sms, EnumConsentPurpose.Transactional, isAllowed: false),
            CancellationToken.None);

        // Suppression yalniz pazarlama amaci icin olusur.
        Assert.True(result.IsSuccess);
        Assert.Empty(suppressions.Items);
    }

    [Fact]
    public async Task SetConsent_AyniDegerTekrarAtanirda_EventUretilmez()
    {
        var preference = new CustomerCommunicationPreference(
            BusinessId, CustomerId, EnumConsentChannel.Sms, EnumConsentPurpose.Marketing, isAllowed: false);
        preference.ClearDomainEvents();

        var preferences = new FakeRepository<CustomerCommunicationPreference>(new[] { preference });
        var handler = NewSetHandler(TestCurrentUserService.For(OwnerId), preferences);

        await handler.Handle(
            NewSetCommand(EnumConsentChannel.Sms, EnumConsentPurpose.Marketing, isAllowed: false),
            CancellationToken.None);

        Assert.Empty(preference.DomainEvents);
    }
}
