using AppointMan.Application.Features.Inventory.Commands.ConsumeServiceMaterials;
using AppointMan.Application.Features.Inventory.Commands.CreateProduct;
using AppointMan.Application.Features.Inventory.Commands.DefineServiceMaterial;
using AppointMan.Application.Features.Inventory.Commands.RecordStockMovement;
using AppointMan.Application.Features.Inventory.Queries.GetLowStockProducts;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Modul 3 - Stok: urun/ SKU benzersizligi, stok bakiyesi ve negatif stok
/// politikasi, referans bazli cift isleme engeli, hizmet recetesi ve
/// randevu malzeme tuketimi kurallari.
/// </summary>
public class InventoryTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();

    private static readonly DateTime UtcNow = new(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    // ===== Yardimcilar =====

    private static FakeRepository<Business> NewBusinessRepo()
        => new(new[] { TestData.NewBusiness(OwnerId, BusinessId) });

    private static TestCurrentUserService OwnerUser()
        => TestCurrentUserService.For(OwnerId);

    private static TestCurrentUserService OtherUser()
        => TestCurrentUserService.For(OtherUserId);

    private static StubDateTimeService NewDateTime()
        => new(UtcNow);

    private static Product NewProduct(string sku = "SKU-1", decimal minimumStock = 0m, Guid? id = null)
    {
        var product = new Product(id ?? ProductId, sku, "Test Urunu", BusinessId, 10m, 20m, minimumStock);
        product.ClearDomainEvents();
        return product;
    }

    private static CreateProductCommand NewCreateProductCommand(string sku = "SKU-1")
        => new()
        {
            BusinessId = BusinessId,
            Sku = sku,
            Name = "Yeni Urun",
            PurchasePrice = 10m,
            SalePrice = 20m,
            MinimumStock = 0m,
            Unit = "adet"
        };

    private static RecordStockMovementCommand NewMovementCommand(
        EnumStockMovementType type,
        decimal quantity,
        Guid? productId = null,
        string? referenceType = null,
        Guid? referenceId = null)
        => new()
        {
            BusinessId = BusinessId,
            ProductId = productId ?? ProductId,
            MovementType = type,
            Quantity = quantity,
            UnitCost = null,
            ReferenceType = referenceType,
            ReferenceId = referenceId
        };

    private static Guid ProductId { get; } = Guid.NewGuid();

    // ===== Urun olusturma =====

    [Fact]
    public async Task CreateProduct_ShouldSucceed_FirstTime()
    {
        var products = new FakeRepository<Product>();
        var sut = new CreateProductCommandHandler(products, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await sut.Handle(NewCreateProductCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(products.Items);
        Assert.Equal("SKU-1", products.Items[0].Sku);
        Assert.True(products.Items[0].IsActive);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenSkuAlreadyExists()
    {
        // Ayni SKU ile ikinci urun tanimlama denemesi CONFLICT uretmeli.
        var products = new FakeRepository<Product>(new[] { NewProduct("SKU-1") });
        var sut = new CreateProductCommandHandler(products, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());

        var result = await sut.Handle(NewCreateProductCommand("sku-1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Single(products.Items);
    }

    [Fact]
    public async Task CreateProduct_ShouldFail_WhenCallerIsNotOwner()
    {
        var products = new FakeRepository<Product>();
        var sut = new CreateProductCommandHandler(products, NewBusinessRepo(), new FakeUnitOfWork(), OtherUser());

        var result = await sut.Handle(NewCreateProductCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(products.Items);
    }

    // ===== Stok hareketleri: bakiye ve negatif stok =====

    private static (RecordStockMovementCommandHandler Handler, FakeRepository<StockMovement> Movements) NewMovementSut(
        TestCurrentUserService? user = null)
    {
        var movements = new FakeRepository<StockMovement>();
        var products = new FakeRepository<Product>(new[] { NewProduct() });
        var handler = new RecordStockMovementCommandHandler(
            movements, products, NewBusinessRepo(), new FakeUnitOfWork(), user ?? OwnerUser(), NewDateTime());
        return (handler, movements);
    }

    [Fact]
    public async Task RecordStockMovement_ShouldKeepBalance_WhenMovementsRecorded()
    {
        // +100 PurchaseIn ve -40 SaleOut sonrasi bakiye 60 olmali.
        var (sut, movements) = NewMovementSut();

        var purchase = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.PurchaseIn, 100m), CancellationToken.None);
        var sale = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.SaleOut, 40m), CancellationToken.None);

        Assert.True(purchase.IsSuccess);
        Assert.True(sale.IsSuccess);
        Assert.Equal(60m, movements.Items.Sum(m => m.Quantity));
    }

    [Fact]
    public async Task RecordStockMovement_ShouldFail_WhenBalanceWouldGoNegative()
    {
        // Bakiye 60 iken 1000 adet cikis denemesi reddedilmeli.
        var (sut, movements) = NewMovementSut();

        await sut.Handle(NewMovementCommand(EnumStockMovementType.PurchaseIn, 100m), CancellationToken.None);
        await sut.Handle(NewMovementCommand(EnumStockMovementType.SaleOut, 40m), CancellationToken.None);

        var result = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.SaleOut, 1000m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        // Reddedilen hareket deftere yazilmamis olmali.
        Assert.Equal(60m, movements.Items.Sum(m => m.Quantity));
    }

    [Fact]
    public async Task RecordStockMovement_ShouldSignQuantities_ByMovementType()
    {
        // Giris hareketleri pozitif, cikis hareketleri negatif isaretli saklanmali.
        var (sut, movements) = NewMovementSut();

        await sut.Handle(NewMovementCommand(EnumStockMovementType.PurchaseIn, 50m), CancellationToken.None);
        await sut.Handle(NewMovementCommand(EnumStockMovementType.CustomerReturn, 5m), CancellationToken.None);
        await sut.Handle(NewMovementCommand(EnumStockMovementType.Waste, 3m), CancellationToken.None);
        await sut.Handle(NewMovementCommand(EnumStockMovementType.ServiceConsumption, 2m), CancellationToken.None);

        Assert.Equal(+50m, movements.Items[0].Quantity);
        Assert.Equal(+5m, movements.Items[1].Quantity);
        Assert.Equal(-3m, movements.Items[2].Quantity);
        Assert.Equal(-2m, movements.Items[3].Quantity);
    }

    [Fact]
    public async Task RecordStockMovement_ShouldFail_WhenSameReferenceProcessedTwice()
    {
        // Dokuman 18.5: ayni (ReferenceType, ReferenceId) ile hareket iki kez islenmemeli.
        var (sut, movements) = NewMovementSut();
        var referenceId = Guid.NewGuid();

        var first = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.PurchaseIn, 10m,
                referenceType: "Sale", referenceId: referenceId),
            CancellationToken.None);
        var second = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.PurchaseIn, 10m,
                referenceType: "Sale", referenceId: referenceId),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(movements.Items);
    }

    [Fact]
    public async Task RecordStockMovement_ShouldFail_WhenCallerIsNotOwner()
    {
        var (sut, movements) = NewMovementSut(OtherUser());

        var result = await sut.Handle(
            NewMovementCommand(EnumStockMovementType.PurchaseIn, 10m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FORBIDDEN", result.Error!.Code);
        Assert.Empty(movements.Items);
    }

    // ===== Hizmet recetesi =====

    private static (DefineServiceMaterialCommandHandler Handler, FakeRepository<ServiceMaterial> Materials) NewMaterialSut()
    {
        var materials = new FakeRepository<ServiceMaterial>();
        var services = new FakeRepository<Service>(new[] { TestData.NewService(BusinessId, ServiceIdForMaterial) });
        var products = new FakeRepository<Product>(new[] { NewProduct() });
        var handler = new DefineServiceMaterialCommandHandler(
            materials, services, products, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser());
        return (handler, materials);
    }

    private static DefineServiceMaterialCommand NewMaterialCommand()
        => new()
        {
            BusinessId = BusinessId,
            ServiceId = ServiceIdForMaterial,
            ProductId = ProductId,
            QuantityPerService = 2m,
            Unit = "adet"
        };

    private static Guid ServiceIdForMaterial { get; } = Guid.NewGuid();

    [Fact]
    public async Task DefineServiceMaterial_ShouldFail_WhenAlreadyDefined()
    {
        // Ayni (ServiceId, ProductId) cifti ikinci kez tanimlanamaz.
        var (sut, materials) = NewMaterialSut();

        var first = await sut.Handle(NewMaterialCommand(), CancellationToken.None);
        var second = await sut.Handle(NewMaterialCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        Assert.Single(materials.Items);
    }

    // ===== Randevu malzeme tuketimi =====

    private static (ConsumeServiceMaterialsCommandHandler Handler, FakeRepository<StockMovement> Movements) NewConsumeSut(
        EnumAppointmentStatus appointmentStatus,
        decimal initialStock = 100m)
    {
        var movements = new FakeRepository<StockMovement>();
        if (initialStock > 0)
        {
            movements.AddAsync(new StockMovement(
                BusinessId, ProductId, EnumStockMovementType.PurchaseIn, initialStock, 10m, UtcNow));
        }

        var products = new FakeRepository<Product>(new[] { NewProduct() });
        var appointments = new FakeRepository<Appointment>(new[]
        {
            TestData.NewAppointment(
                ConsumeAppointmentId,
                Guid.NewGuid(),
                BusinessId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                UtcNow.AddDays(-1),
                appointmentStatus)
        });
        var handler = new ConsumeServiceMaterialsCommandHandler(
            movements, products, appointments, NewBusinessRepo(), new FakeUnitOfWork(), OwnerUser(), NewDateTime());
        return (handler, movements);
    }

    private static Guid ConsumeAppointmentId { get; } = Guid.NewGuid();

    private static ConsumeServiceMaterialsCommand NewConsumeCommand(decimal quantity = 2m)
        => new()
        {
            BusinessId = BusinessId,
            AppointmentId = ConsumeAppointmentId,
            Materials = new List<ConsumeMaterialLine>
            {
                new() { ProductId = ProductId, Quantity = quantity }
            }
        };

    [Fact]
    public async Task ConsumeServiceMaterials_ShouldFail_WhenAppointmentIsNotCompleted()
    {
        // Randevu Completed degilse tuketim islenmez.
        var (sut, movements) = NewConsumeSut(EnumAppointmentStatus.Pending, initialStock: 0m);

        var result = await sut.Handle(NewConsumeCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Empty(movements.Items);
    }

    [Fact]
    public async Task ConsumeServiceMaterials_ShouldCreateServiceConsumptionMovements_WhenCompleted()
    {
        var (sut, movements) = NewConsumeSut(EnumAppointmentStatus.Completed);

        var result = await sut.Handle(NewConsumeCommand(2m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
        var consumption = Assert.Single(
            movements.Items.Where(m => m.MovementType == EnumStockMovementType.ServiceConsumption));
        Assert.Equal(-2m, consumption.Quantity);
        Assert.Equal("Appointment", consumption.ReferenceType);
        Assert.Equal(ConsumeAppointmentId, consumption.ReferenceId);
    }

    [Fact]
    public async Task ConsumeServiceMaterials_ShouldFail_WhenConsumedTwiceForSameAppointment()
    {
        // Ayni randevu icin tuketim ikinci kez islenemez (cift isleme engeli).
        var (sut, movements) = NewConsumeSut(EnumAppointmentStatus.Completed);

        var first = await sut.Handle(NewConsumeCommand(), CancellationToken.None);
        var second = await sut.Handle(NewConsumeCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("CONFLICT", second.Error!.Code);
        // Ikinci deneme yeni tuketim hareketi uretmemeli.
        Assert.Single(
            movements.Items.Where(m => m.MovementType == EnumStockMovementType.ServiceConsumption));
    }

    [Fact]
    public async Task ConsumeServiceMaterials_ShouldFail_WhenStockInsufficient()
    {
        // Bakiye 3 iken 5 adet tuketim denemesi reddedilmeli.
        var (sut, movements) = NewConsumeSut(EnumAppointmentStatus.Completed, initialStock: 3m);

        var result = await sut.Handle(NewConsumeCommand(5m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Single(movements.Items); // yalnizca baslangic stok girisi vardir
    }

    // ===== Kritik stok raporu =====

    [Fact]
    public async Task GetLowStockProducts_ShouldList_BelowMinimumStock()
    {
        // MinimumStock 10 olan urun bakiyesi 5 ise rapora girmeli.
        var product = NewProduct("SKU-LOW", minimumStock: 10m);
        var products = new FakeRepository<Product>(new[] { product });
        var movements = new FakeRepository<StockMovement>(new[]
        {
            new StockMovement(BusinessId, product.Id, EnumStockMovementType.PurchaseIn, 5m, 10m, UtcNow)
        });
        var sut = new GetLowStockProductsQueryHandler(products, movements, NewBusinessRepo(), OwnerUser());

        var result = await sut.Handle(new GetLowStockProductsQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(5m, dto.Balance);
        Assert.Equal(10m, dto.MinimumStock);
        Assert.Equal(5m, dto.Shortage);
    }
}
