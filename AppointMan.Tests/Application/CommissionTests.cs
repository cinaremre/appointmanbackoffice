using AppointMan.Application.Features.Commissions.Commands.AccrueCommission;
using AppointMan.Application.Features.Commissions.Commands.CreateCommissionRule;
using AppointMan.Application.Features.Commissions.Commands.ReverseCommissionAccrual;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Modul 4 - Personel primi: kural tanimi, tahsilat tabanli tahakkuk
/// hesabi (yuzde/sabit), kural penceresi kontrolu, en spesifik kural
/// secimi ve tahakkuk ters cevirme kurallari.
/// </summary>
public class CommissionTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static readonly DateTime UtcNow = new(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    // ===== Yardimcilar =====

    private static FakeRepository<Business> NewBusinessRepo()
        => new(new[] { TestData.NewBusiness(OwnerId, BusinessId) });

    private static FakeRepository<Employee> NewEmployeeRepo()
        => new(new[] { TestData.NewEmployee(BusinessId, EmployeeId) });

    private static TestCurrentUserService OwnerUser()
        => TestCurrentUserService.For(OwnerId);

    private static TestCurrentUserService OtherUser()
        => TestCurrentUserService.For(OtherUserId);

    private static StubDateTimeService NewDateTime()
        => new(UtcNow);

    /// <summary>Isletme geneli aktif kural; pencere su ani kapsar.</summary>
    private static CommissionRule NewRule(
        decimal rate = 0.20m,
        decimal fixedAmount = 0m,
        EnumCommissionCalculationType type = EnumCommissionCalculationType.ServicePercent,
        Guid? employeeId = null,
        Guid? serviceId = null,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null)
    {
        var rule = new CommissionRule(
            BusinessId,
            type,
            rate,
            fixedAmount,
            effectiveFrom ?? UtcNow.AddDays(-1),
            effectiveTo,
            employeeId,
            serviceId,
            productId: null);
        rule.ClearDomainEvents();
        return rule;
    }

    private static AccrueCommissionCommand NewAccrueCommand(decimal baseAmount = 100m)
        => new()
        {
            BusinessId = BusinessId,
            EmployeeId = EmployeeId,
            ServiceId = ServiceId,
            BaseAmount = baseAmount
        };

    private static (AccrueCommissionCommandHandler Handler, FakeRepository<CommissionAccrual> Accruals) NewAccrueSut(
        FakeRepository<CommissionRule>? rules = null,
        TestCurrentUserService? user = null)
    {
        var accruals = new FakeRepository<CommissionAccrual>();
        var handler = new AccrueCommissionCommandHandler(
            rules ?? new FakeRepository<CommissionRule>(),
            accruals,
            NewEmployeeRepo(),
            NewBusinessRepo(),
            new FakeUnitOfWork(),
            user ?? OwnerUser(),
            NewDateTime());
        return (handler, accruals);
    }

    // ===== Kural tanimi =====

    [Fact]
    public async Task CreateRule_ShouldSucceed_WithValidWindow()
    {
        var rules = new FakeRepository<CommissionRule>();
        var sut = new CreateCommissionRuleCommandHandler(
            rules, NewEmployeeRepo(),
            new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceId) }),
            new FakeRepository<Product>(), NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await sut.Handle(new CreateCommissionRuleCommand
        {
            BusinessId = BusinessId,
            EmployeeId = EmployeeId,
            ServiceId = ServiceId,
            CalculationType = EnumCommissionCalculationType.ServicePercent,
            Rate = 0.20m,
            FixedAmount = 0m,
            EffectiveFrom = UtcNow.AddDays(-1),
            EffectiveTo = UtcNow.AddYears(1)
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(rules.Items);
    }

    [Fact]
    public async Task CreateRule_ShouldFail_WhenEffectiveToBeforeEffectiveFrom()
    {
        var rules = new FakeRepository<CommissionRule>();
        var sut = new CreateCommissionRuleCommandHandler(
            rules, NewEmployeeRepo(), new FakeRepository<Service>(), new FakeRepository<Product>(),
            NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await sut.Handle(new CreateCommissionRuleCommand
        {
            BusinessId = BusinessId,
            CalculationType = EnumCommissionCalculationType.ServicePercent,
            Rate = 0.20m,
            FixedAmount = 0m,
            EffectiveFrom = UtcNow,
            EffectiveTo = UtcNow.AddDays(-1)
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(rules.Items);
    }

    [Fact]
    public async Task CreateRule_ShouldFail_WhenCallerIsNotOwner()
    {
        var rules = new FakeRepository<CommissionRule>();
        var sut = new CreateCommissionRuleCommandHandler(
            rules, NewEmployeeRepo(), new FakeRepository<Service>(), new FakeRepository<Product>(),
            NewBusinessRepo(), new FakeUnitOfWork(), OtherUser());

        var result = await sut.Handle(new CreateCommissionRuleCommand
        {
            BusinessId = BusinessId,
            CalculationType = EnumCommissionCalculationType.ServicePercent,
            Rate = 0.20m,
            FixedAmount = 0m,
            EffectiveFrom = UtcNow
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(rules.Items);
    }

    // ===== Tahakkuk hesabi =====

    [Fact]
    public async Task Accrue_PercentRule_ShouldCalculateFromBaseAmount()
    {
        // %20 kurali: 100 TL tahsil edilen baz -> 20 TL prim.
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule(rate: 0.20m) });
        var (sut, accruals) = NewAccrueSut(rules);

        var result = await sut.Handle(NewAccrueCommand(baseAmount: 100m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var accrual = Assert.Single(accruals.Items);
        Assert.Equal(100m, accrual.BaseAmount);
        Assert.Equal(20m, accrual.CommissionAmount);
        Assert.Equal(EnumCommissionAccrualStatus.Accrued, accrual.Status);
    }

    [Fact]
    public async Task Accrue_FixedRule_ShouldUseFixedAmount()
    {
        // Hizmet basina sabit 50 TL kurali.
        var rules = new FakeRepository<CommissionRule>(new[]
        {
            NewRule(fixedAmount: 50m, type: EnumCommissionCalculationType.FixedPerService)
        });
        var (sut, accruals) = NewAccrueSut(rules);

        var result = await sut.Handle(NewAccrueCommand(baseAmount: 100m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var accrual = Assert.Single(accruals.Items);
        Assert.Equal(50m, accrual.CommissionAmount);
    }

    [Fact]
    public async Task Accrue_ShouldPreferMostSpecificRule()
    {
        // Genel %10 ve calisan+hizmet ozel %25 kurali ayni anda aktifse
        // en spesifik olan (%25) kullanilmali.
        var rules = new FakeRepository<CommissionRule>(new[]
        {
            NewRule(rate: 0.10m),
            NewRule(rate: 0.25m, employeeId: EmployeeId, serviceId: ServiceId)
        });
        var (sut, accruals) = NewAccrueSut(rules);

        var result = await sut.Handle(NewAccrueCommand(baseAmount: 100m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var accrual = Assert.Single(accruals.Items);
        Assert.Equal(25m, accrual.CommissionAmount);
        Assert.Equal(rules.Items.First(r => r.Rate == 0.25m).Id, accrual.RuleId);
    }

    [Fact]
    public async Task Accrue_ShouldFail_WhenNoEffectiveRule()
    {
        // Kural penceresi kapanmissa (EffectiveTo gecmis) tahakkuk reddedilmeli.
        var rules = new FakeRepository<CommissionRule>(new[]
        {
            NewRule(rate: 0.20m, effectiveFrom: UtcNow.AddDays(-10), effectiveTo: UtcNow.AddDays(-1))
        });
        var (sut, accruals) = NewAccrueSut(rules);

        var result = await sut.Handle(NewAccrueCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(accruals.Items);
    }

    [Fact]
    public async Task Accrue_ShouldFail_WhenCallerIsNotOwner()
    {
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule() });
        var (sut, accruals) = NewAccrueSut(rules, OtherUser());

        var result = await sut.Handle(NewAccrueCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(accruals.Items);
    }

    // ===== Tahakkuk ters cevirme =====

    [Fact]
    public async Task Reverse_ShouldSucceed_WhenAccrualIsAccrued()
    {
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule() });
        var (accrueSut, accruals) = NewAccrueSut(rules);

        var accrueResult = await accrueSut.Handle(NewAccrueCommand(), CancellationToken.None);
        Assert.True(accrueResult.IsSuccess);

        var reverseSut = new ReverseCommissionAccrualCommandHandler(
            accruals, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await reverseSut.Handle(new ReverseCommissionAccrualCommand
        {
            BusinessId = BusinessId,
            AccrualId = accrueResult.Value
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EnumCommissionAccrualStatus.Reversed, accruals.Items[0].Status);
    }

    [Fact]
    public async Task Reverse_ShouldFail_WhenAccrualIsPaid()
    {
        // Odenmis (Paid) tahakkuk ters cevrilemez.
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule() });
        var (accrueSut, accruals) = NewAccrueSut(rules);

        var accrueResult = await accrueSut.Handle(NewAccrueCommand(), CancellationToken.None);
        Assert.True(accrueResult.IsSuccess);

        var accrual = accruals.Items[0];
        accrual.MarkAsPaid();

        var reverseSut = new ReverseCommissionAccrualCommandHandler(
            accruals, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await reverseSut.Handle(new ReverseCommissionAccrualCommand
        {
            BusinessId = BusinessId,
            AccrualId = accrueResult.Value
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Equal(EnumCommissionAccrualStatus.Paid, accruals.Items[0].Status);
    }

    [Fact]
    public async Task Reverse_ShouldFail_WhenAccrualAlreadyReversed()
    {
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule() });
        var (accrueSut, accruals) = NewAccrueSut(rules);

        var accrueResult = await accrueSut.Handle(NewAccrueCommand(), CancellationToken.None);

        var reverseSut = new ReverseCommissionAccrualCommandHandler(
            accruals, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var first = await reverseSut.Handle(new ReverseCommissionAccrualCommand
        {
            BusinessId = BusinessId,
            AccrualId = accrueResult.Value
        }, CancellationToken.None);
        var second = await reverseSut.Handle(new ReverseCommissionAccrualCommand
        {
            BusinessId = BusinessId,
            AccrualId = accrueResult.Value
        }, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", second.Error!.Code);
    }

    [Fact]
    public async Task Reverse_ShouldFail_WhenCallerIsNotOwner()
    {
        var rules = new FakeRepository<CommissionRule>(new[] { NewRule() });
        var (accrueSut, accruals) = NewAccrueSut(rules);

        var accrueResult = await accrueSut.Handle(NewAccrueCommand(), CancellationToken.None);
        Assert.True(accrueResult.IsSuccess);

        var reverseSut = new ReverseCommissionAccrualCommandHandler(
            accruals, NewBusinessRepo(), new FakeUnitOfWork(), OtherUser());

        var result = await reverseSut.Handle(new ReverseCommissionAccrualCommand
        {
            BusinessId = BusinessId,
            AccrualId = accrueResult.Value
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Equal(EnumCommissionAccrualStatus.Accrued, accruals.Items[0].Status);
    }
}
