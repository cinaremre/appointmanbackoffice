using Rezervise.Application.Features.Businesses.Queries.GetBusinessById;
using Rezervise.Domain.Entities;
using Rezervise.Domain.ValueObjects;
using Rezervise.Tests;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// İşletme detayı (GET /api/Business/{id}): kaydederken gönderilen adres nesnesi
/// geri dönmeli — cityId, street, postalCode, district, city, country — ve
/// phoneNumber/email { "value": "..." } biçiminde olmalı.
/// </summary>
public class GetBusinessByIdQueryHandlerTests
{
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ShouldReturnAddressAsObject_WithCityAndDistrictNames()
    {
        var business = NewBusiness();
        // Kadıköy/İstanbul, posta kodu yok — kaydederken gönderilen şekil.
        business.UpdateAddress(new Address(34, 438, "Caferağa Mahallesi, Moda Caddesi, No: 45, D: 3", null));

        var city = new City(34, "İstanbul", "TR");
        var district = new District(438, 34, "Kadıköy");

        var sut = CreateSut(business, city, district);

        var result = await sut.Handle(new GetBusinessByIdQuery { BusinessId = BusinessId }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value;

        // phoneNumber / email { "value": ... } nesnesi.
        Assert.NotNull(dto.PhoneNumber);
        Assert.Equal("+905550000000", dto.PhoneNumber.Value);
        Assert.NotNull(dto.Email);
        Assert.Equal("business@test.com", dto.Email.Value);

        // Adres nesnesi: uygulama düzenleme ekranında cityId'yi buradan okur.
        Assert.NotNull(dto.Address);
        Assert.Equal(34, dto.Address.CityId);
        Assert.Equal("İstanbul", dto.Address.City);
        Assert.Equal(438, dto.Address.DistrictId);
        Assert.Equal("Kadıköy", dto.Address.District);
        Assert.Equal("Türkiye", dto.Address.Country);
        Assert.Equal("Caferağa Mahallesi, Moda Caddesi, No: 45, D: 3", dto.Address.Street);
        Assert.Null(dto.Address.PostalCode);

        // Tek metin adres posta kodu olmadan düzgün oluşmalı.
        Assert.Equal("Caferağa Mahallesi, Moda Caddesi, No: 45, D: 3", dto.Address.FullAddress);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBusinessDoesNotExist()
    {
        var sut = CreateSut(null, new City(34, "İstanbul", "TR"), new District(438, 34, "Kadıköy"));

        var result = await sut.Handle(
            new GetBusinessByIdQuery { BusinessId = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    private static Business NewBusiness()
        => TestData.NewBusiness(OwnerId, BusinessId);

    private static GetBusinessByIdQueryHandler CreateSut(
        Business? business, City city, District district)
    {
        return new GetBusinessByIdQueryHandler(
            new FakeRepository<Business>(business is null ? Array.Empty<Business>() : new[] { business }),
            new FakeRepository<City>(new[] { city }),
            new FakeRepository<District>(new[] { district }));
    }
}
