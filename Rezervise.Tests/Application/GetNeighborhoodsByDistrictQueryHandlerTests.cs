using Rezervise.Application.Features.Neighborhoods.Queries.GetByDistrict;
using Rezervise.Domain.Entities;
using Rezervise.Tests.Fakes;

namespace Rezervise.Tests.Application;

/// <summary>
/// GET /api/Address/districts/{districtId}/neighborhoods: ilçe seçildiğinde
/// o ilçenin tüm mahalleleri (semt ayrımı olmadan) tek listede dönmeli.
/// </summary>
public class GetNeighborhoodsByDistrictQueryHandlerTests
{
    private const int KadikoyId = 438;
    private const int BesiktasId = 439;

    [Fact]
    public async Task Handle_ShouldReturnAllNeighborhoodsOfDistrict()
    {
        var kadikoy = new District(KadikoyId, 34, "Kadıköy");

        var caferaga = new Locality(1984, KadikoyId, "Caferağa");
        var osmanaga = new Locality(1985, KadikoyId, "Osmanağa");
        var besiktasLocality = new Locality(2000, BesiktasId, "Sinanpaşa");

        var moda = new Neighborhood(51001, 1984, "Moda");
        var caferagaMah = new Neighborhood(51002, 1984, "Caferağa");
        var osmanagaMah = new Neighborhood(51003, 1985, "Osmanağa");
        var baskaIlce = new Neighborhood(51004, 2000, "Sinanpaşa");

        var sut = CreateSut(
            kadikoy,
            new[] { caferaga, osmanaga, besiktasLocality },
            new[] { moda, caferagaMah, osmanagaMah, baskaIlce });

        var result = await sut.Handle(
            new GetNeighborhoodsByDistrictQuery { DistrictId = KadikoyId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);

        // Başka ilçenin mahallesi listeye girmez.
        Assert.DoesNotContain(result.Value, d => d.Id == 51004);

        // İsim sırasına göre döner ve semt kimliği korunur.
        Assert.Equal(new[] { "Caferağa", "Moda", "Osmanağa" }, result.Value.Select(d => d.Name).ToArray());
        Assert.Equal(1984, result.Value.Single(d => d.Id == 51002).LocalityId);
        Assert.Equal(1985, result.Value.Single(d => d.Id == 51003).LocalityId);
    }

    [Fact]
    public async Task Handle_ShouldFilterInactive_WhenOnlyActiveRequested()
    {
        var kadikoy = new District(KadikoyId, 34, "Kadıköy");
        var caferaga = new Locality(1984, KadikoyId, "Caferağa");

        var aktif = new Neighborhood(51001, 1984, "Moda");
        var pasif = new Neighborhood(51002, 1984, "Eski Mahalle");
        pasif.Deactivate();

        var sut = CreateSut(kadikoy, new[] { caferaga }, new[] { aktif, pasif });

        var result = await sut.Handle(
            new GetNeighborhoodsByDistrictQuery { DistrictId = KadikoyId, OnlyActive = true },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal("Moda", dto.Name);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenDistrictDoesNotExist()
    {
        var sut = CreateSut(null, Array.Empty<Locality>(), Array.Empty<Neighborhood>());

        var result = await sut.Handle(
            new GetNeighborhoodsByDistrictQuery { DistrictId = 999999 },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenDistrictIdIsInvalid()
    {
        var sut = CreateSut(null, Array.Empty<Locality>(), Array.Empty<Neighborhood>());

        var result = await sut.Handle(
            new GetNeighborhoodsByDistrictQuery { DistrictId = 0 },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
    }

    private static GetNeighborhoodsByDistrictQueryHandler CreateSut(
        District? district,
        IEnumerable<Locality> localities,
        IEnumerable<Neighborhood> neighborhoods)
    {
        return new GetNeighborhoodsByDistrictQueryHandler(
            new FakeRepository<District>(district is null ? Array.Empty<District>() : new[] { district }),
            new FakeRepository<Locality>(localities),
            new FakeRepository<Neighborhood>(neighborhoods));
    }
}
