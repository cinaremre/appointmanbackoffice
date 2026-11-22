using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminPayments;
using Rezervise.Application.Features.Admin.Finance.Queries.GetAdminSales;
using Rezervise.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application.BackOffice;

/// <summary>
/// Backoffice finans/stok okuma (dokuman Modul 16-17) testleri:
/// satis filtreleri ve toplam, iade ayrimi, musteri paketi kalan hak hesabi.
/// Tum sorgular salt okunurdur; testler yalnizca okuma semasi dogrular.
/// </summary>
public class AdminFinanceTests
{
    private static readonly Guid BusinessId1 = Guid.NewGuid();
    private static readonly Guid BusinessId2 = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();

    private static readonly DateTime Nov1 = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Nov5 = new(2026, 11, 5, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Nov10 = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Dec5 = new(2026, 12, 5, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NowUtc = new(2026, 11, 21, 10, 0, 0, DateTimeKind.Utc);

    // ===== Yardimcilar =====

    /// <summary>Tek satirli, belirtilen tutarda satis uretir.</summary>
    private static Sale NewSale(Guid businessId, DateTime soldAt, decimal amount)
    {
        var sale = new Sale(businessId, soldAt, "TRY");
        sale.AddLine(new SaleLine(
            sale.Id,
            EnumSaleItemType.Service,
            ServiceId,
            productId: null,
            quantity: 1m,
            unitPrice: amount,
            discountAmount: 0m,
            taxAmount: 0m,
            employeeId: EmployeeId));

        return sale;
    }

    // ===== Satis listesi =====

    [Fact]
    public async Task GetAdminSales_ShouldFilterByBusinessAndDateAndSumGrandTotals()
    {
        // 1. Isletme: Kasim'da iki satis + Aralik'ta kapsam disi bir satis.
        // 2. Isletme: Kasim'da tek satis (filtre disi kalmali).
        var sales = new FakeRepository<Sale>(new[]
        {
            NewSale(BusinessId1, Nov1, 100m),
            NewSale(BusinessId1, Nov10, 250m),
            NewSale(BusinessId1, Dec5, 999m),
            NewSale(BusinessId2, Nov5, 500m)
        });

        var sut = new GetAdminSalesQueryHandler(sales);

        var result = await sut.Handle(
            new GetAdminSalesQuery
            {
                BusinessId = BusinessId1,
                FromDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate = new DateTime(2026, 11, 30, 23, 59, 59, DateTimeKind.Utc)
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);

        // Yalniz 1. isletmenin kasim satislari donmeli.
        Assert.Equal(2L, result.Value!.TotalCount);
        Assert.All(result.Value.Items, s => Assert.Equal(BusinessId1, s.BusinessId));

        // Toplam, filtre kapsaminin tamami (100 + 250).
        Assert.Equal(350m, result.Value.TotalGrandTotal);

        // Tarihe gore ters siralama: 10 kasim once.
        Assert.Equal(Nov10, result.Value.Items[0].SoldAt);
        Assert.Equal(250m, result.Value.Items[0].GrandTotal);
        Assert.Equal(Nov1, result.Value.Items[1].SoldAt);
        Assert.Equal(100m, result.Value.Items[1].GrandTotal);
    }

    [Fact]
    public async Task GetAdminSales_WithoutFilters_ShouldSumAllGrandTotals()
    {
        var sales = new FakeRepository<Sale>(new[]
        {
            NewSale(BusinessId1, Nov1, 100m),
            NewSale(BusinessId2, Nov5, 500m),
            NewSale(BusinessId1, Dec5, 999m)
        });

        var sut = new GetAdminSalesQueryHandler(sales);

        var result = await sut.Handle(new GetAdminSalesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(3L, result.Value!.TotalCount);
        Assert.Equal(1599m, result.Value.TotalGrandTotal);
    }

    // ===== Odeme listesi: iade ayrimi =====

    [Fact]
    public async Task GetAdminPayments_ShouldSeparateRefundsFromNormalPayments()
    {
        var saleId = Guid.NewGuid();

        var original = new Payment(BusinessId1, saleId, 100m, EnumPaymentMethod.Cash, Nov1);
        var refund = Payment.CreateRefund(original, 40m, Nov10);

        var payments = new FakeRepository<Payment>(new[] { original, refund });
        var sut = new GetAdminPaymentsQueryHandler(payments);

        // 1. Yalnizca iadeler: tek kayit, orijinale bagli.
        var refunds = await sut.Handle(
            new GetAdminPaymentsQuery { IsRefund = true },
            CancellationToken.None);

        Assert.True(refunds.IsSuccess, refunds.Error?.Message);
        var refundItem = Assert.Single(refunds.Value!.Items);
        Assert.Equal(refund.Id, refundItem.Id);
        Assert.True(refundItem.IsRefund);
        Assert.Equal(40m, refundItem.Amount);
        Assert.Equal(original.Id, refundItem.RefersToPaymentId);

        // 2. Yalnizca normal odemeler: orijinal kayit.
        var normals = await sut.Handle(
            new GetAdminPaymentsQuery { IsRefund = false },
            CancellationToken.None);

        Assert.True(normals.IsSuccess, normals.Error?.Message);
        var normalItem = Assert.Single(normals.Value!.Items);
        Assert.Equal(original.Id, normalItem.Id);
        Assert.False(normalItem.IsRefund);
        Assert.Null(normalItem.RefersToPaymentId);

        // 3. Filtresiz: ikisi birlikte.
        var all = await sut.Handle(new GetAdminPaymentsQuery(), CancellationToken.None);

        Assert.True(all.IsSuccess, all.Error?.Message);
        Assert.Equal(2L, all.Value!.TotalCount);
    }

    [Fact]
    public async Task GetAdminPayments_ShouldFilterByBusinessAndMethod()
    {
        var saleId = Guid.NewGuid();

        var cashPayment = new Payment(BusinessId1, saleId, 100m, EnumPaymentMethod.Cash, Nov1);
        var cardPayment = new Payment(BusinessId1, saleId, 200m, EnumPaymentMethod.BankCard, Nov5);
        var otherBusinessPayment = new Payment(
            BusinessId2, Guid.NewGuid(), 300m, EnumPaymentMethod.Cash, Nov10);

        var payments = new FakeRepository<Payment>(new[] { cashPayment, cardPayment, otherBusinessPayment });
        var sut = new GetAdminPaymentsQueryHandler(payments);

        var result = await sut.Handle(
            new GetAdminPaymentsQuery
            {
                BusinessId = BusinessId1,
                PaymentMethod = EnumPaymentMethod.Cash
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(cashPayment.Id, item.Id);
        Assert.Equal(EnumPaymentMethod.Cash, item.PaymentMethod);
    }

    // ===== Musteri paketi kalan haklari =====

    [Fact]
    public async Task GetAdminCustomerPackages_ShouldComputeRemainingRights()
    {
        // 8 seanslik paket: 2 tuketim + 1 aktif rezervasyon -> kalan 5.
        var package = new ServicePackage("8 Seans Paketi", 500m, 30, BusinessId1);
        package.AddItem(new ServicePackageItem(package.Id, ServiceId, 8));

        var customerPackage = new CustomerPackage(
            BusinessId1, CustomerId, package.Id, package.ValidityDays, NowUtc);

        // Tuketimlerden biri iptal: hak hesabina girmez.
        var cancelledConsumption = PackageUsage.Consume(
            customerPackage.Id, ServiceId, Guid.NewGuid(), NowUtc);
        cancelledConsumption.Cancel(NowUtc);

        var usages = new FakeRepository<PackageUsage>(new PackageUsage[]
        {
            PackageUsage.Consume(customerPackage.Id, ServiceId, Guid.NewGuid(), NowUtc),
            PackageUsage.Consume(customerPackage.Id, ServiceId, Guid.NewGuid(), NowUtc),
            cancelledConsumption,
            PackageUsage.Reserve(customerPackage.Id, ServiceId, Guid.NewGuid(), NowUtc)
        });

        var sut = new GetAdminCustomerPackagesQueryHandler(
            new FakeRepository<CustomerPackage>(new[] { customerPackage }),
            new FakeRepository<ServicePackage>(new[] { package }),
            usages);

        var result = await sut.Handle(
            new GetAdminCustomerPackagesQuery { BusinessId = BusinessId1 },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(customerPackage.Id, item.Id);
        Assert.Equal(package.Id, item.PackageId);
        Assert.Equal(8, item.Purchased);
        Assert.Equal(2, item.Consumed);
        Assert.Equal(1, item.Reserved);
        Assert.Equal(5, item.Remaining);
        Assert.Equal(EnumCustomerPackageStatus.Active, item.Status);
    }

    [Fact]
    public async Task GetAdminCustomerPackages_ShouldFilterByCustomerAndStatus()
    {
        var package = new ServicePackage("4 Seans Paketi", 250m, 30, BusinessId1);
        package.AddItem(new ServicePackageItem(package.Id, ServiceId, 4));

        var activePurchase = new CustomerPackage(
            BusinessId1, CustomerId, package.Id, package.ValidityDays, NowUtc);
        var otherCustomerPurchase = new CustomerPackage(
            BusinessId1, Guid.NewGuid(), package.Id, package.ValidityDays, NowUtc);

        var sut = new GetAdminCustomerPackagesQueryHandler(
            new FakeRepository<CustomerPackage>(new[] { activePurchase, otherCustomerPurchase }),
            new FakeRepository<ServicePackage>(new[] { package }),
            new FakeRepository<PackageUsage>());

        var result = await sut.Handle(
            new GetAdminCustomerPackagesQuery
            {
                BusinessId = BusinessId1,
                CustomerId = CustomerId,
                Status = EnumCustomerPackageStatus.Active
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(CustomerId, item.CustomerId);
        Assert.Equal(4, item.Purchased);
        Assert.Equal(4, item.Remaining);
    }
}
