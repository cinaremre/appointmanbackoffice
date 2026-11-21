using AppointMan.Application.Features.Sales.Commands.CreateSale;
using AppointMan.Application.Features.Sales.Commands.RefundPayment;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Satis olusturma ve iade akislari: satir toplami dogrulamasi, odeme
/// siniri, randevu durumu ve yetki kontrolleri (JWT kimligi esas alinir,
/// govde degeri degil).
/// </summary>
public class SalesCashTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid SaleId = Guid.NewGuid();
    private static readonly Guid AppointmentId = Guid.NewGuid();

    private static readonly DateTime SoldAt = new(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);

    // Varsayilan satir: 2 x 100 - 20 indirim + 10 KDV = 190 GrandTotal.
    private static List<CreateSaleCommand.SaleLineInput> DefaultLines() => new()
    {
        new CreateSaleCommand.SaleLineInput
        {
            ItemType = EnumSaleItemType.Service,
            ServiceId = ServiceId,
            Quantity = 2m,
            UnitPrice = 100m,
            DiscountAmount = 20m,
            TaxAmount = 10m,
            EmployeeId = EmployeeId
        }
    };

    private static CreateSaleCommand NewCommand(
        List<CreateSaleCommand.SaleLineInput>? lines = null,
        List<CreateSaleCommand.PaymentInput>? payments = null,
        Guid? appointmentId = null)
        => new()
        {
            BusinessId = BusinessId,
            CustomerId = CustomerId,
            AppointmentId = appointmentId,
            Lines = lines ?? DefaultLines(),
            Payments = payments ?? new List<CreateSaleCommand.PaymentInput>
            {
                new() { Amount = 190m, PaymentMethod = EnumPaymentMethod.Cash }
            }
        };

    private static (FakeRepository<Sale> Sales, FakeRepository<Payment> Payments, CreateSaleCommandHandler Handler)
        CreateSaleHandler(
            TestCurrentUserService currentUser,
            IEnumerable<Appointment>? appointments = null,
            IEnumerable<CashSession>? sessions = null)
    {
        var sales = new FakeRepository<Sale>();
        var payments = new FakeRepository<Payment>();

        var handler = new CreateSaleCommandHandler(
            new FakeRepository<Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeRepository<Appointment>(appointments ?? Enumerable.Empty<Appointment>()),
            sales,
            payments,
            new FakeRepository<CashSession>(sessions ?? Enumerable.Empty<CashSession>()),
            new FakeUnitOfWork(),
            currentUser,
            new StubDateTimeService(SoldAt));

        return (sales, payments, handler);
    }

    private static (FakeRepository<Payment> Payments, FakeRepository<CashMovement> Movements, RefundPaymentCommandHandler Handler)
        CreateRefundHandler(
            TestCurrentUserService currentUser,
            IEnumerable<Payment>? payments = null,
            IEnumerable<CashSession>? sessions = null)
    {
        var paymentRepo = new FakeRepository<Payment>(payments ?? Enumerable.Empty<Payment>());
        var movements = new FakeRepository<CashMovement>();

        var handler = new RefundPaymentCommandHandler(
            paymentRepo,
            new FakeRepository<Business>(new[] { TestData.NewBusiness(OwnerId, BusinessId) }),
            new FakeRepository<CashSession>(sessions ?? Enumerable.Empty<CashSession>()),
            movements,
            new FakeUnitOfWork(),
            currentUser,
            new StubDateTimeService(SoldAt));

        return (paymentRepo, movements, handler);
    }

    [Fact]
    public async Task CreateSale_ShouldCompleteSale_WhenCallerIsBusinessOwner()
    {
        var (sales, payments, handler) = CreateSaleHandler(TestCurrentUserService.For(OwnerId));

        var result = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var sale = Assert.Single(sales.Items);
        Assert.Equal(EnumSaleStatus.Completed, sale.Status);
        Assert.Equal(200m, sale.Subtotal);
        Assert.Equal(20m, sale.DiscountTotal);
        Assert.Equal(10m, sale.TaxTotal);
        Assert.Equal(190m, sale.GrandTotal);
        Assert.Equal("TRY", sale.Currency);

        // Satir toplami: Quantity * UnitPrice - Discount + Tax
        var line = Assert.Single(sale.Lines);
        Assert.Equal(190m, line.LineTotal);

        var payment = Assert.Single(payments.Items);
        Assert.Equal(190m, payment.Amount);
        Assert.Equal(EnumPaymentMethod.Cash, payment.PaymentMethod);
    }

    [Fact]
    public async Task CreateSale_ShouldFail_WhenCallerIsNotBusinessOwner()
    {
        // Govde isletme sahibi gibi gorunse bile JWT kimligi esas alinir.
        var (sales, _, handler) = CreateSaleHandler(TestCurrentUserService.For(OtherUserId));

        var result = await handler.Handle(NewCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(sales.Items);
    }

    [Fact]
    public async Task CreateSale_ShouldFail_WhenPaymentExceedsGrandTotal()
    {
        var (sales, payments, handler) = CreateSaleHandler(TestCurrentUserService.For(OwnerId));

        // GrandTotal 190; odemeler 150 + 50 = 200.
        var command = NewCommand(payments: new List<CreateSaleCommand.PaymentInput>
        {
            new() { Amount = 150m, PaymentMethod = EnumPaymentMethod.Cash },
            new() { Amount = 50m, PaymentMethod = EnumPaymentMethod.BankCard }
        });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Empty(sales.Items);
        Assert.Empty(payments.Items);
    }

    [Fact]
    public async Task CreateSale_ShouldFail_WhenLineDiscountExceedsLineGross()
    {
        var (sales, _, handler) = CreateSaleHandler(TestCurrentUserService.For(OwnerId));

        // Satir brutu 200; indirim 300 olamaz.
        var command = NewCommand(lines: new List<CreateSaleCommand.SaleLineInput>
        {
            new()
            {
                ItemType = EnumSaleItemType.Service,
                ServiceId = ServiceId,
                Quantity = 2m,
                UnitPrice = 100m,
                DiscountAmount = 300m,
                TaxAmount = 0m,
                EmployeeId = EmployeeId
            }
        });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(sales.Items);
    }

    [Fact]
    public async Task CreateSale_ShouldFail_WhenAppointmentIsNotCompleted()
    {
        var appointment = TestData.NewAppointment(
            AppointmentId,
            CustomerId,
            BusinessId,
            ServiceId,
            EmployeeId,
            new DateTime(2026, 11, 21, 9, 0, 0));

        var (sales, _, handler) = CreateSaleHandler(
            TestCurrentUserService.For(OwnerId),
            new[] { appointment });

        var result = await handler.Handle(NewCommand(appointmentId: AppointmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(sales.Items);
    }

    [Fact]
    public async Task RefundPayment_ShouldCreateReversePayment_WhenCashRefund()
    {
        var session = new CashSession(Guid.NewGuid(), BusinessId, OwnerId, 100m, SoldAt);
        session.ClearDomainEvents();

        var payment = new Payment(BusinessId, SaleId, 500m, EnumPaymentMethod.Cash, SoldAt, session.Id);
        payment.ClearDomainEvents();

        var (payments, movements, handler) = CreateRefundHandler(
            TestCurrentUserService.For(OwnerId),
            new[] { payment },
            new[] { session });

        var result = await handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id, Amount = 100m, Reason = "Musteri vazgecti" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        // Ters kayit: negatif tutar yerine IsRefund bayrakli ayri odeme.
        var refund = payments.Items.Single(p => p.IsRefund);
        Assert.Equal(100m, refund.Amount);
        Assert.Equal(payment.Id, refund.RefersToPaymentId);
        Assert.Equal(session.Id, refund.CashSessionId);

        // Nakit iade kasaya cikis hareketi yazdi; ReferenceId ters kayda bagli.
        var movement = Assert.Single(movements.Items);
        Assert.Equal(EnumCashMovementType.CashOut, movement.MovementType);
        Assert.Equal(100m, movement.Amount);
        Assert.Equal(refund.Id, movement.ReferenceId);
    }

    [Fact]
    public async Task RefundPayment_ShouldFail_WhenAmountExceedsOriginal()
    {
        var payment = new Payment(BusinessId, SaleId, 500m, EnumPaymentMethod.Cash, SoldAt);
        payment.ClearDomainEvents();

        var (payments, movements, handler) = CreateRefundHandler(
            TestCurrentUserService.For(OwnerId),
            new[] { payment });

        var result = await handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id, Amount = 600m },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        Assert.Empty(payments.Items.Where(p => p.IsRefund));
        Assert.Empty(movements.Items);
    }

    [Fact]
    public async Task RefundPayment_ShouldFail_WhenCumulativeRefundsExceedOriginal()
    {
        // Kart odemede acik kasa oturumu gerekmediginden akis izole test edilir.
        var payment = new Payment(BusinessId, SaleId, 500m, EnumPaymentMethod.BankCard, SoldAt);
        payment.ClearDomainEvents();

        var (payments, _, handler) = CreateRefundHandler(
            TestCurrentUserService.For(OwnerId),
            new[] { payment });

        // Ilk iade 300 basarili.
        var first = await handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id, Amount = 300m },
            CancellationToken.None);
        Assert.True(first.IsSuccess);

        // Ikinci iade 250: kalan 200'yi asar.
        var second = await handler.Handle(
            new RefundPaymentCommand { PaymentId = payment.Id, Amount = 250m },
            CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("VALIDATION_ERROR", second.Error!.Code);
        Assert.Equal(2, payments.Items.Count); // orijinal + tek iade
    }

    [Fact]
    public void Sale_CompletedStatus_ShouldBeTerminal()
    {
        // Terminal durum matrisi: Completed'ten hicbir gecis yok.
        var sale = new Sale(BusinessId, SoldAt);
        sale.AddLine(new SaleLine(
            sale.Id, EnumSaleItemType.Service, ServiceId, null,
            1m, 100m, 0m, 0m, EmployeeId));

        sale.Complete();

        Assert.Equal(EnumSaleStatus.Completed, sale.Status);
        Assert.False(Sale.IsValidStatusTransition(
            EnumSaleStatus.Completed, EnumSaleStatus.Cancelled));
        Assert.False(Sale.IsValidStatusTransition(
            EnumSaleStatus.Completed, EnumSaleStatus.Completed));
        Assert.Throws<DomainException>(() => sale.Cancel());
    }

    [Fact]
    public void Sale_OnlyPendingStatus_ShouldAllowCompletionAndCancellation()
    {
        Assert.True(Sale.IsValidStatusTransition(
            EnumSaleStatus.Pending, EnumSaleStatus.Completed));
        Assert.True(Sale.IsValidStatusTransition(
            EnumSaleStatus.Pending, EnumSaleStatus.Cancelled));
        Assert.False(Sale.IsValidStatusTransition(
            EnumSaleStatus.Cancelled, EnumSaleStatus.Completed));
    }
}
