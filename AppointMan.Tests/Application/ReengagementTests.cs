using AppointMan.Application.Features.Reengagement.Commands.MarkReengagementNotified;
using AppointMan.Application.Features.Reengagement.Queries.GetReengagementCandidates;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Kural bazli geri cagirma aday hesabinin izole testleri (dokuman Modul 13).
/// </summary>
public class ReengagementTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid OtherCustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    private const string Template = "Merhaba {{CustomerName}}, {{ServiceName}} icin randevunuzu {{BookingUrl}} adresinden yenileyebilirsiniz.";

    private static BusinessReengagementRule NewRule(
        int intervalDays = 30,
        int leadDays = 0,
        int cooldownDays = 0,
        Guid? serviceId = null)
        => new(BusinessId, serviceId, Template, intervalDays, leadDays, maxMessagesPerCycle: 1, cooldownDays, isEnabled: true);

    private static CustomerServiceHistory NewHistory(
        Guid customerId,
        DateTime lastCompletedAt,
        int visits = 1,
        int medianIntervalDays = 10)
    {
        var history = new CustomerServiceHistory(BusinessId, customerId, ServiceId);

        // Son ziyaretten geriye dogru esit aralikli tamamlama tarihleri uret.
        var dates = Enumerable.Range(0, visits)
            .Select(i => lastCompletedAt.AddDays(-i * medianIntervalDays))
            .ToList();

        history.RecalculateFrom(dates, Now);
        return history;
    }

    private static Appointment NewFutureAppointment(Guid customerId)
        => TestData.NewAppointment(
            Guid.NewGuid(),
            customerId,
            BusinessId,
            ServiceId,
            EmployeeId,
            Now.AddHours(24),
            EnumAppointmentStatus.Pending);

    private static GetReengagementCandidatesQueryHandler NewSut(
        TestCurrentUserService currentUser,
        FakeRepository<BusinessReengagementRule>? rules = null,
        FakeRepository<CustomerServiceHistory>? histories = null,
        FakeRepository<Appointment>? appointments = null,
        FakeRepository<ReengagementAttempt>? attempts = null)
    {
        return new GetReengagementCandidatesQueryHandler(
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            rules ?? new FakeRepository<BusinessReengagementRule>(),
            histories ?? new FakeRepository<CustomerServiceHistory>(),
            appointments ?? new FakeRepository<Appointment>(),
            attempts ?? new FakeRepository<ReengagementAttempt>(),
            new FakeRepository<CustomerCommunicationPreference>(),
            currentUser,
            new StubDateTimeService(Now));
    }

    [Fact]
    public async Task GetCandidates_AralikDolduysa_AdayListelenir()
    {
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { NewRule(serviceId: ServiceId) });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-40))
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var candidate = Assert.Single(result.Value);
        Assert.Equal(CustomerId, candidate.CustomerId);
        Assert.Equal(ServiceId, candidate.ServiceId);
        // Sablon degiskenleri bilincli olarak doldurulmadan donmeli.
        Assert.Equal(Template, candidate.SuggestedMessage);
    }

    [Fact]
    public async Task GetCandidates_OnBaslangicGunu_IleAdayEsigiErkenler()
    {
        // 40 gun gecmis, aralik 45, on baslangic 10 -> esik 35 -> aday.
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { NewRule(intervalDays: 45, leadDays: 10, serviceId: ServiceId) });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-40))
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetCandidates_AralikDolmadiysa_AdayListelenmez()
    {
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { NewRule(serviceId: ServiceId) });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-10))
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetCandidates_GelecekRandevusuOlan_AdayListesindeYok()
    {
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { NewRule(serviceId: ServiceId) });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-40)),
            NewHistory(OtherCustomerId, Now.AddDays(-40))
        });
        var appointments = new FakeRepository<Appointment>(new[]
        {
            NewFutureAppointment(CustomerId)
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories, appointments);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Gelecek randevusu olmayan musteriler listelenmeli.
        var candidate = Assert.Single(result.Value);
        Assert.Equal(OtherCustomerId, candidate.CustomerId);
    }

    [Fact]
    public async Task GetCandidates_CooldownIcindekiDeneme_AdayiEler()
    {
        var rule = NewRule(cooldownDays: 7, serviceId: ServiceId);
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { rule });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-40))
        });
        var attempts = new FakeRepository<ReengagementAttempt>(new[]
        {
            NewNotifiedAttempt(rule.Id, Now.AddDays(-2))
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories, attempts: attempts);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetCandidates_CooldownDolmusDeneme_AdayiElemez()
    {
        var rule = NewRule(cooldownDays: 7, serviceId: ServiceId);
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { rule });
        var histories = new FakeRepository<CustomerServiceHistory>(new[]
        {
            NewHistory(CustomerId, Now.AddDays(-40))
        });
        var attempts = new FakeRepository<ReengagementAttempt>(new[]
        {
            NewNotifiedAttempt(rule.Id, Now.AddDays(-10))
        });

        var sut = NewSut(TestCurrentUserService.For(OwnerId), rules, histories, attempts: attempts);

        var result = await sut.Handle(new GetReengagementCandidatesQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    private static ReengagementAttempt NewNotifiedAttempt(Guid ruleId, DateTime triggeredAt)
    {
        var attempt = new ReengagementAttempt(BusinessId, CustomerId, ServiceId, ruleId, triggeredAt);
        attempt.MarkNotified();
        return attempt;
    }

    // ============= Domain istatistik testleri =============

    [Fact]
    public void RecalculateFrom_TekZiyarette_MedyanNullDir()
    {
        var history = new CustomerServiceHistory(BusinessId, CustomerId, ServiceId);

        history.RecalculateFrom(new List<DateTime> { Now.AddDays(-40) }, Now);

        Assert.Equal(1, history.VisitCount);
        Assert.Null(history.MedianIntervalDays);
        Assert.Equal(Now.AddDays(-40), history.LastCompletedAt);
    }

    [Fact]
    public void RecalculateFrom_UcZiyarette_MedyanHesaplanir()
    {
        var history = new CustomerServiceHistory(BusinessId, CustomerId, ServiceId);

        // Araliklar: 10 ve 20 gun -> medyan 15.
        var dates = new List<DateTime>
        {
            Now.AddDays(-30),
            Now.AddDays(-20),
            Now
        };

        history.RecalculateFrom(dates, Now);

        Assert.Equal(3, history.VisitCount);
        Assert.Equal(15, history.MedianIntervalDays);
        Assert.Equal(Now.Date, history.LastCompletedAt.Date);
    }

    // ============= MarkNotified =============

    [Fact]
    public async Task MarkNotified_DenemeKaydiniNotifiedDurumundaOlusturur()
    {
        var rule = NewRule(serviceId: ServiceId);
        var rules = new FakeRepository<BusinessReengagementRule>(new[] { rule });
        var attempts = new FakeRepository<ReengagementAttempt>();

        var handler = new MarkReengagementNotifiedCommandHandler(
            new FakeRepository<AppointMan.Domain.Entities.Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            rules,
            attempts,
            new FakeUnitOfWork(),
            TestCurrentUserService.For(OwnerId),
            new StubDateTimeService(Now));

        var command = new MarkReengagementNotifiedCommand
        {
            BusinessId = BusinessId,
            CustomerId = CustomerId,
            ServiceId = ServiceId,
            RuleId = rule.Id
        };

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var attempt = Assert.Single(attempts.Items);
        Assert.Equal(EnumReengagementStatus.Notified, attempt.Status);
        Assert.Equal(rule.Id, attempt.RuleId);
        Assert.Equal(CustomerId, attempt.CustomerId);
    }
}
