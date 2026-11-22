using Rezervise.Application.Features.Cash.Commands.AddCashMovement;
using Rezervise.Application.Features.Cash.Commands.CloseCashSession;
using Rezervise.Application.Features.Cash.Commands.CreateCashRegister;
using Rezervise.Application.Features.Cash.Commands.OpenCashSession;
using Rezervise.Application.Features.Cash.Commands.SetCashRegisterActive;
using Rezervise.Application.Features.Sales.Commands.CreateSale;
using Rezervise.Application.Features.Sales.Commands.RefundPayment;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// Kasa oturumu akislari: acik oturumda ikinci acis engeli, gun sonu
/// beklenen nakit hesabi (kart tahsilat haric) ve kapali oturuma hareket.
/// </summary>
public class CashSessionTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private static readonly DateTime DayStart = new(2026, 11, 21, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tum handler'larin ayni fake repository ornelerini paylastigi test
    /// duzenegi; boylece akis testleri gercek handler zinciriyle kosar.
    /// </summary>
    private sealed class Harness
    {
        public FakeRepository<Business> Businesses { get; } =
            new(new[] { TestData.NewBusiness(OwnerId, BusinessId) });

        public FakeRepository<CashRegister> Registers { get; } = new();
        public FakeRepository<CashSession> Sessions { get; } = new();
        public FakeRepository<CashMovement> Movements { get; } = new();
        public FakeRepository<Sale> Sales { get; } = new();
        public FakeRepository<Payment> Payments { get; } = new();

        public StubDateTimeService Clock { get; } = new(DayStart);

        public OpenCashSessionCommandHandler OpenHandler(TestCurrentUserService user)
            => new(Registers, Sessions, Businesses, new FakeUnitOfWork(), user, Clock);

        public CloseCashSessionCommandHandler CloseHandler(TestCurrentUserService user)
            => new(Sessions, Businesses, Movements, Payments, new FakeUnitOfWork(), user, Clock);

        public AddCashMovementCommandHandler MovementHandler(TestCurrentUserService user)
            => new(Sessions, Businesses, Movements, new FakeUnitOfWork(), user);

        public CreateSaleCommandHandler SaleHandler(TestCurrentUserService user)
            => new(Businesses,
                  new FakeRepository<Appointment>(),
                  Sales,
                  Payments,
                  Sessions,
                  new FakeUnitOfWork(),
                  user,
                  Clock);

        public RefundPaymentCommandHandler RefundHandler(TestCurrentUserService user)
            => new(Payments, Businesses, Sessions, Movements, new FakeUnitOfWork(), user, Clock);

        public CreateCashRegisterCommandHandler RegisterHandler(TestCurrentUserService user)
            => new(Businesses, Registers, new FakeUnitOfWork(), user);

        public SetCashRegisterActiveCommandHandler SetActiveHandler(TestCurrentUserService user)
            => new(Registers, Businesses, new FakeUnitOfWork(), user);
    }

    private static CashRegister NewRegister(Harness harness)
    {
        var register = new CashRegister("Kasa 1", BusinessId);
        register.ClearDomainEvents();
        harness.Registers.AddAsync(register).GetAwaiter().GetResult();
        return register;
    }

    [Fact]
    public async Task OpenCashSession_ShouldFail_WhenSessionAlreadyOpen()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        var first = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 100m },
            CancellationToken.None);

        Assert.True(first.IsSuccess);

        var second = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 50m },
            CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(harness.Sessions.Items);
    }

    [Fact]
    public async Task OpenCashSession_ShouldFail_WhenRegisterIsDeactivated()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        register.Deactivate();

        var result = await harness.OpenHandler(TestCurrentUserService.For(OwnerId)).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 100m },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Empty(harness.Sessions.Items);
    }

    [Fact]
    public async Task SetCashRegisterActive_ShouldToggleRegisterState()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        // Olusturma sonrasi kasa aktif; deaktif et ve geri ac.
        var deactivate = await harness.SetActiveHandler(owner).Handle(
            new SetCashRegisterActiveCommand { CashRegisterId = register.Id, IsActive = false },
            CancellationToken.None);

        Assert.True(deactivate.IsSuccess);
        Assert.False(register.IsActive);

        var activate = await harness.SetActiveHandler(owner).Handle(
            new SetCashRegisterActiveCommand { CashRegisterId = register.Id, IsActive = true },
            CancellationToken.None);

        Assert.True(activate.IsSuccess);
        Assert.True(register.IsActive);
    }

    [Fact]
    public async Task CreateCashRegister_ShouldFail_WhenCallerIsNotBusinessOwner()
    {
        var harness = new Harness();

        var result = await harness.RegisterHandler(TestCurrentUserService.For(OtherUserId)).Handle(
            new CreateCashRegisterCommand { BusinessId = BusinessId, Name = "Kasa 1" },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(harness.Registers.Items);
    }

    /// <summary>
    /// Gun sonu hesabi: acilis 100 + nakit tahsilat 500 + giris 50
    /// - cikis 30 - nakit iade 20 = beklenen 600. Sayilan 590 ile
    /// fark -10. Kart tahsilat (200) beklenen nakide dahil degil.
    /// </summary>
    [Fact]
    public async Task CloseCashSession_ShouldComputeExpectedCash_WithCardPaymentsExcluded()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        // 1. Oturumu 100 acilis nakdiyle ac.
        var open = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 100m },
            CancellationToken.None);

        Assert.True(open.IsSuccess);
        var sessionId = open.Value!;

        // 2. 700 tutarinda satis: 500 nakit + 200 kart.
        var sale = await harness.SaleHandler(owner).Handle(
            new CreateSaleCommand
            {
                BusinessId = BusinessId,
                Lines = new List<CreateSaleCommand.SaleLineInput>
                {
                    new()
                    {
                        ItemType = EnumSaleItemType.Service,
                        ServiceId = Guid.NewGuid(),
                        Quantity = 7m,
                        UnitPrice = 100m,
                        DiscountAmount = 0m,
                        TaxAmount = 0m,
                        EmployeeId = Guid.NewGuid()
                    }
                },
                Payments = new List<CreateSaleCommand.PaymentInput>
                {
                    new() { Amount = 500m, PaymentMethod = EnumPaymentMethod.Cash },
                    new() { Amount = 200m, PaymentMethod = EnumPaymentMethod.BankCard }
                }
            },
            CancellationToken.None);

        Assert.True(sale.IsSuccess);

        // 3. Manuel kasa hareketleri: 50 giris, 30 cikis.
        var cashIn = await harness.MovementHandler(owner).Handle(
            new AddCashMovementCommand
            {
                CashSessionId = sessionId,
                MovementType = EnumCashMovementType.CashIn,
                Amount = 50m,
                Reason = "Navlon"
            },
            CancellationToken.None);

        Assert.True(cashIn.IsSuccess);

        var cashOut = await harness.MovementHandler(owner).Handle(
            new AddCashMovementCommand
            {
                CashSessionId = sessionId,
                MovementType = EnumCashMovementType.CashOut,
                Amount = 30m,
                Reason = "Kirtasiye"
            },
            CancellationToken.None);

        Assert.True(cashOut.IsSuccess);

        // 4. 20 nakit iade (ters kayit + kasa cikis hareketi).
        var originalCashPayment = harness.Payments.Items
            .Single(p => !p.IsRefund && p.PaymentMethod == EnumPaymentMethod.Cash);

        var refund = await harness.RefundHandler(owner).Handle(
            new RefundPaymentCommand { PaymentId = originalCashPayment.Id, Amount = 20m, Reason = "Iade" },
            CancellationToken.None);

        Assert.True(refund.IsSuccess);

        // 5. Oturumu 590 sayilan nakitle kapat.
        var close = await harness.CloseHandler(owner).Handle(
            new CloseCashSessionCommand { CashSessionId = sessionId, CountedCash = 590m },
            CancellationToken.None);

        Assert.True(close.IsSuccess);

        // Beklenen: 100 + 500 + 50 - 30 - 20 = 600; kart (200) haric.
        Assert.Equal(600m, close.Value!.ExpectedCash);
        Assert.Equal(590m, close.Value.CountedCash);
        Assert.Equal(-10m, close.Value.Difference);

        var session = harness.Sessions.Items.Single(s => s.Id == sessionId);
        Assert.Equal(EnumCashSessionStatus.Closed, session.Status);
        Assert.Equal(600m, session.ExpectedCash);
        Assert.Equal(-10m, session.Difference);
        Assert.Equal(OwnerId, session.ClosedBy);
    }

    [Fact]
    public async Task AddCashMovement_ShouldFail_WhenSessionIsClosed()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        var open = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 0m },
            CancellationToken.None);

        Assert.True(open.IsSuccess);

        var close = await harness.CloseHandler(owner).Handle(
            new CloseCashSessionCommand { CashSessionId = open.Value!, CountedCash = 0m },
            CancellationToken.None);

        Assert.True(close.IsSuccess);

        var movement = await harness.MovementHandler(owner).Handle(
            new AddCashMovementCommand
            {
                CashSessionId = open.Value!,
                MovementType = EnumCashMovementType.CashIn,
                Amount = 50m,
                Reason = "Gec giris"
            },
            CancellationToken.None);

        Assert.True(movement.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", movement.Error!.Code);
        Assert.Empty(harness.Movements.Items);
    }

    [Fact]
    public async Task CloseCashSession_ShouldFail_WhenSessionIsAlreadyClosed()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        var open = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 100m },
            CancellationToken.None);

        var firstClose = await harness.CloseHandler(owner).Handle(
            new CloseCashSessionCommand { CashSessionId = open.Value!, CountedCash = 100m },
            CancellationToken.None);

        Assert.True(firstClose.IsSuccess);

        var secondClose = await harness.CloseHandler(owner).Handle(
            new CloseCashSessionCommand { CashSessionId = open.Value!, CountedCash = 100m },
            CancellationToken.None);

        Assert.True(secondClose.IsFailure);
        Assert.Equal("CONFLICT", secondClose.Error!.Code);
    }

    [Fact]
    public async Task CloseCashSession_ShouldFail_WhenCallerIsNotBusinessOwner()
    {
        var harness = new Harness();
        var register = NewRegister(harness);
        var owner = TestCurrentUserService.For(OwnerId);

        var open = await harness.OpenHandler(owner).Handle(
            new OpenCashSessionCommand { CashRegisterId = register.Id, OpeningCash = 100m },
            CancellationToken.None);

        Assert.True(open.IsSuccess);

        var close = await harness.CloseHandler(TestCurrentUserService.For(OtherUserId)).Handle(
            new CloseCashSessionCommand { CashSessionId = open.Value!, CountedCash = 100m },
            CancellationToken.None);

        Assert.True(close.IsFailure);
        Assert.Equal("FORBIDDEN", close.Error!.Code);

        var session = harness.Sessions.Items.Single(s => s.Id == open.Value);
        Assert.Equal(EnumCashSessionStatus.Open, session.Status);
    }
}
