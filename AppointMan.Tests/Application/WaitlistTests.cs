using AppointMan.Application.Features.Waitlist.Commands.AcceptWaitlistOffer;
using AppointMan.Application.Features.Waitlist.Commands.CancelWaitlistEntry;
using AppointMan.Application.Features.Waitlist.Commands.CreateWaitlistOffers;
using AppointMan.Application.Features.Waitlist.Commands.JoinWaitlist;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Bekleme listesi (dokuman Modul 10, yarim otomatik mod) akis testleri:
/// kayit cakismasi, teklif uretme onceligi, teklif kabul kurallari ve yetki.
/// </summary>
public class WaitlistTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerAId = Guid.NewGuid();
    private static readonly Guid CustomerBId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 21, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>Slot ve kayit tarihleri: kayitlar [Now+1, Now+3] araliginda, slot Now+2'de.</summary>
    private static readonly DateTime EarliestDate = NowUtc.AddDays(1);
    private static readonly DateTime LatestDate = NowUtc.AddDays(3);
    private static readonly DateTime SlotStart = NowUtc.AddDays(2);

    /// <summary>
    /// SUT paketi: handler'lar ayni repo orneklerini paylasir. Varsayilan
    /// kimlikler: Join/Accept/Cancel musterinin kendisi (A), CreateOffers
    /// isletme sahibi. Testler istedigi kimligi asiri yukleme ile degistirir.
    /// </summary>
    private sealed class WaitlistSut
    {
        public JoinWaitlistCommandHandler Join = null!;
        public CreateWaitlistOffersCommandHandler CreateOffers = null!;
        public AcceptWaitlistOfferCommandHandler Accept = null!;
        public CancelWaitlistEntryCommandHandler Cancel = null!;

        public FakeRepository<WaitlistEntry> Entries = null!;
        public FakeRepository<WaitlistOffer> Offers = null!;
        public FakeRepository<Appointment> Appointments = null!;
        public StubDateTimeService DateTime = null!;
    }

    private static WaitlistSut CreateSut(
        IEnumerable<Appointment>? appointments = null,
        Guid? joinCallerId = null,
        Guid? offersCallerId = null,
        Guid? acceptCallerId = null,
        Guid? cancelCallerId = null)
    {
        var business = TestData.NewBusiness(OwnerId, BusinessId);
        var service = TestData.NewService(BusinessId, ServiceId);

        var entries = new FakeRepository<WaitlistEntry>();
        var offers = new FakeRepository<WaitlistOffer>();
        var appointmentRepo = new FakeRepository<Appointment>(appointments ?? Array.Empty<Appointment>());
        var businesses = new FakeRepository<Business>(new[] { business });
        var services = new FakeRepository<Service>(new[] { service });
        var users = new FakeRepository<User>(new[]
        {
            TestData.NewUser(CustomerAId),
            TestData.NewUser(CustomerBId)
        });

        var joinUser = TestCurrentUserService.For(joinCallerId ?? CustomerAId);
        var offersUser = TestCurrentUserService.For(offersCallerId ?? OwnerId);
        var acceptUser = TestCurrentUserService.For(acceptCallerId ?? CustomerAId);
        var cancelUser = TestCurrentUserService.For(cancelCallerId ?? CustomerAId);
        var dateTime = new StubDateTimeService(NowUtc);

        return new WaitlistSut
        {
            Join = new JoinWaitlistCommandHandler(
                entries, businesses, services, users,
                new FakeUnitOfWork(), joinUser),
            CreateOffers = new CreateWaitlistOffersCommandHandler(
                entries, offers, businesses, services,
                new FakeUnitOfWork(), offersUser, dateTime),
            Accept = new AcceptWaitlistOfferCommandHandler(
                offers, entries, appointmentRepo,
                new FakeUnitOfWork(), acceptUser, dateTime),
            Cancel = new CancelWaitlistEntryCommandHandler(
                entries, businesses,
                new FakeUnitOfWork(), cancelUser, dateTime),

            Entries = entries,
            Offers = offers,
            Appointments = appointmentRepo,
            DateTime = dateTime
        };
    }

    private static JoinWaitlistCommand NewJoinCommand(Guid? customerId = null)
        => new()
        {
            BusinessId = BusinessId,
            CustomerId = customerId ?? Guid.Empty,
            ServiceId = ServiceId,
            EarliestDate = EarliestDate,
            LatestDate = LatestDate,
            PreferredTimeRange = "09:00-17:00"
        };

    /// <summary>Kaydi dogrudan olusturur (Join handler atlanir); once olusturulan once siraya girer.</summary>
    private static WaitlistEntry NewEntry(Guid customerId)
        => new(BusinessId, customerId, ServiceId, EarliestDate, LatestDate);

    private static CreateWaitlistOffersCommand NewOffersCommand(int maxOffers = 10)
        => new()
        {
            BusinessId = BusinessId,
            SlotStart = SlotStart,
            ServiceId = ServiceId,
            MaxOffers = maxOffers
        };

    [Fact]
    public async Task Join_ShouldFail_WhenActiveEntryAlreadyExists()
    {
        var sut = CreateSut();

        var first = await sut.Join.Handle(NewJoinCommand(), CancellationToken.None);
        var second = await sut.Join.Handle(NewJoinCommand(), CancellationToken.None);

        // Ayni musteri + ayni hizmet icin ikinci aktif kayit acilamaz.
        Assert.True(first.IsSuccess, first.Error?.Message);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(sut.Entries.Items);
    }

    [Fact]
    public async Task Join_ShouldAllowOwner_ToJoinForAnotherCustomer()
    {
        var sut = CreateSut(joinCallerId: OwnerId);

        var result = await sut.Join.Handle(NewJoinCommand(CustomerBId), CancellationToken.None);

        // Isletme sahibi musteri adina kayit acabilir.
        Assert.True(result.IsSuccess, result.Error?.Message);
        var entry = Assert.Single(sut.Entries.Items);
        Assert.Equal(CustomerBId, entry.CustomerId);
        Assert.Equal(EnumWaitlistEntryStatus.Active, entry.Status);
    }

    [Fact]
    public async Task CreateOffers_ShouldFail_WhenCallerIsNotOwner()
    {
        var sut = CreateSut(offersCallerId: CustomerAId);

        // Kayit olsa bile teklif uretme isletme onay adimidir.
        await sut.Entries.AddAsync(NewEntry(CustomerAId));

        var result = await sut.CreateOffers.Handle(NewOffersCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(sut.Offers.Items);
    }

    [Fact]
    public async Task CreateOffers_ShouldSkipEntries_OutsideDateRange()
    {
        var sut = CreateSut();

        // Kayit [Now+2, Now+3] araliginda; slot Now+1'de -> aralik disi.
        var entry = new WaitlistEntry(
            BusinessId, CustomerAId, ServiceId,
            NowUtc.AddDays(2), NowUtc.AddDays(3));
        await sut.Entries.AddAsync(entry);

        var outOfRangeSlot = NewOffersCommand();
        outOfRangeSlot.SlotStart = NowUtc.AddDays(1);

        var result = await sut.CreateOffers.Handle(outOfRangeSlot, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
        Assert.Empty(sut.Offers.Items);
    }

    [Fact]
    public async Task CreateOffers_ShouldPrioritizeEarlierEntries()
    {
        var sut = CreateSut();

        // Iki musteri sirayla kayit olur; slot her ikisinin araliginda.
        await sut.Entries.AddAsync(NewEntry(CustomerAId));
        await sut.Entries.AddAsync(NewEntry(CustomerBId));

        var result = await sut.CreateOffers.Handle(NewOffersCommand(maxOffers: 1), CancellationToken.None);

        // Once kayit olan A musterisi teklifi alir.
        Assert.True(result.IsSuccess);
        var offerId = Assert.Single(result.Value!);
        var offer = Assert.Single(sut.Offers.Items);
        Assert.Equal(offerId, offer.Id);

        var entryA = sut.Entries.Items.First(e => e.CustomerId == CustomerAId);
        Assert.Equal(entryA.Id, offer.WaitlistEntryId);
    }

    [Fact]
    public async Task Accept_ShouldFail_WhenOfferExpired()
    {
        var sut = CreateSut();

        await sut.Entries.AddAsync(NewEntry(CustomerAId));

        var offersResult = await sut.CreateOffers.Handle(NewOffersCommand(), CancellationToken.None);
        Assert.True(offersResult.IsSuccess);
        var offerId = Assert.Single(offersResult.Value!);

        // Teklif suresi (WaitlistPolicy: 10 dakika) dolduktan sonra kabul edilemez.
        sut.DateTime.UtcNow = NowUtc.AddMinutes(11);

        var acceptResult = await sut.Accept.Handle(
            new AcceptWaitlistOfferCommand { OfferId = offerId },
            CancellationToken.None);

        Assert.True(acceptResult.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", acceptResult.Error!.Code);

        var offer = Assert.Single(sut.Offers.Items);
        Assert.Equal(EnumWaitlistOfferStatus.Pending, offer.Status);
    }

    [Fact]
    public async Task Accept_ShouldFail_WhenSlotTaken()
    {
        var sut = CreateSut();

        await sut.Entries.AddAsync(NewEntry(CustomerAId));

        var offersResult = await sut.CreateOffers.Handle(NewOffersCommand(), CancellationToken.None);
        Assert.True(offersResult.IsSuccess);
        var offerId = Assert.Single(offersResult.Value!);

        // Slot, baska bir musterinin aktif randevusuyla dolduruldu.
        await sut.Appointments.AddAsync(TestData.NewAppointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessId,
            ServiceId,
            EmployeeId,
            SlotStart,
            EnumAppointmentStatus.Confirmed));

        var acceptResult = await sut.Accept.Handle(
            new AcceptWaitlistOfferCommand { OfferId = offerId },
            CancellationToken.None);

        // Atomik yeniden kontrol: kabul aninda slot dolu ise Conflict.
        Assert.True(acceptResult.IsFailure);
        Assert.Equal("CONFLICT", acceptResult.Error!.Code);
    }

    [Fact]
    public async Task Accept_ShouldSupersedeOtherPendingOffers_ForSameSlot()
    {
        var sut = CreateSut();

        await sut.Entries.AddAsync(NewEntry(CustomerAId));
        await sut.Entries.AddAsync(NewEntry(CustomerBId));

        var offersResult = await sut.CreateOffers.Handle(NewOffersCommand(maxOffers: 2), CancellationToken.None);
        Assert.True(offersResult.IsSuccess);
        Assert.Equal(2, offersResult.Value!.Count);

        // A musterisi (once kayit olan) teklifini kabul eder.
        var acceptResult = await sut.Accept.Handle(
            new AcceptWaitlistOfferCommand { OfferId = offersResult.Value[0] },
            CancellationToken.None);

        Assert.True(acceptResult.IsSuccess, acceptResult.Error?.Message);

        var acceptedOffer = sut.Offers.Items.First(o => o.Id == offersResult.Value[0]);
        var otherOffer = sut.Offers.Items.First(o => o.Id == offersResult.Value[1]);

        Assert.Equal(EnumWaitlistOfferStatus.Accepted, acceptedOffer.Status);
        Assert.NotNull(acceptedOffer.AcceptedAt);

        // Ayni slot icin kalan Pending teklif gecersizlesir.
        Assert.Equal(EnumWaitlistOfferStatus.Superseded, otherOffer.Status);

        // Kabul eden musterinin kaydi tamamlanir.
        var entryA = sut.Entries.Items.First(e => e.CustomerId == CustomerAId);
        Assert.Equal(EnumWaitlistEntryStatus.Fulfilled, entryA.Status);
    }

    [Fact]
    public async Task CancelEntry_ShouldFail_WhenCallerIsNeitherCustomerNorOwner()
    {
        var sut = CreateSut(cancelCallerId: CustomerBId);

        // A musterisinin kaydi; caller iliskisiz B musterisi.
        var entry = NewEntry(CustomerAId);
        await sut.Entries.AddAsync(entry);

        var result = await sut.Cancel.Handle(
            new CancelWaitlistEntryCommand { EntryId = entry.Id },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal(EnumWaitlistEntryStatus.Active, entry.Status);
    }

    [Fact]
    public async Task CancelEntry_ShouldAllowCustomer_ToCancelOwnEntry()
    {
        var sut = CreateSut();

        var joinResult = await sut.Join.Handle(NewJoinCommand(), CancellationToken.None);
        Assert.True(joinResult.IsSuccess);

        var entry = Assert.Single(sut.Entries.Items);
        var result = await sut.Cancel.Handle(
            new CancelWaitlistEntryCommand { EntryId = entry.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(EnumWaitlistEntryStatus.Cancelled, entry.Status);
    }
}
