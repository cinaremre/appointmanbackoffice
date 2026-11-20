using AppointMan.Application.Features.Favorites.Commands.AddFavorite;
using AppointMan.Application.Features.Favorites.Commands.RemoveFavorite;
using AppointMan.Application.Features.Favorites.Queries.CheckFavorite;
using AppointMan.Domain.Entities;
using AppointMan.Tests;
using AppointMan.Tests.Fakes;

namespace AppointMan.Tests.Application;

/// <summary>
/// Favori ekle/cikar: ekleme idempotent olmali, soft-delete edilmis kayit
/// yeniden eklemeyi engellememeli, silme aktif kaydi bulmali.
/// </summary>
public class FavoriteCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid OtherBusinessId = Guid.NewGuid();

    // ===== Ekleme =====

    [Fact]
    public async Task Add_ShouldCreateFavorite_FirstTime()
    {
        var (sut, favorites, _) = CreateAddSut();

        var result = await sut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(favorites.Items);
        Assert.Equal(result.Value, favorites.Items[0].Id);
        Assert.False(favorites.Items[0].IsDeleted);
    }

    [Fact]
    public async Task Add_ShouldBeIdempotent_WhenFavoriteAlreadyExists()
    {
        var (sut, favorites, _) = CreateAddSut();

        var first = await sut.Handle(NewAddCommand(), CancellationToken.None);
        var second = await sut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        // Ayni cift icin ayni kayit donmeli; yeni satir uretilmemeli.
        Assert.Equal(first.Value, second.Value);
        Assert.Single(favorites.Items);
        Assert.Single(favorites.Items.Where(f => !f.IsDeleted));
    }

    [Fact]
    public async Task Add_ShouldNotFail_WhenPreviousFavoriteWasSoftDeleted()
    {
        // Filtresiz unique index hatasinin dogrudan regresyon testi:
        // soft-delete edilmis kayit anahtari isgal etmemeli.
        var softDeleted = NewFavorite();
        softDeleted.Delete();
        softDeleted.ClearDomainEvents();

        var favorites = new FakeRepository<Favorite>(new[] { softDeleted });
        var sut = CreateAddHandler(favorites);

        var result = await sut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, favorites.Items.Count);
        Assert.Single(favorites.Items.Where(f => !f.IsDeleted));
        Assert.NotEqual(softDeleted.Id, result.Value);
    }

    [Fact]
    public async Task Add_ShouldCreateSeparateFavorites_ForDifferentBusinesses()
    {
        var (sut, favorites, _) = CreateAddSut();

        var first = await sut.Handle(NewAddCommand(), CancellationToken.None);
        var second = await sut.Handle(NewAddCommand(OtherBusinessId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, favorites.Items.Count);
        Assert.Equal(2, favorites.Items.Count(f => !f.IsDeleted));
    }

    [Fact]
    public async Task Add_ShouldFail_WhenUserDoesNotExist()
    {
        var sut = new AddFavoriteCommandHandler(
            new FakeRepository<Favorite>(),
            new FakeRepository<User>(),
            new FakeRepository<Business>(new[] { TestData.NewBusiness(Guid.NewGuid(), BusinessId) }),
            new FakeUnitOfWork());

        var result = await sut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Add_ShouldFail_WhenBusinessDoesNotExist()
    {
        var sut = new AddFavoriteCommandHandler(
            new FakeRepository<Favorite>(),
            new FakeRepository<User>(new[] { TestData.NewUser(UserId) }),
            new FakeRepository<Business>(),
            new FakeUnitOfWork());

        var result = await sut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    // ===== Silme =====

    [Fact]
    public async Task Remove_ShouldSoftDeleteActiveFavorite()
    {
        var favorite = NewFavorite();
        var sut = new RemoveFavoriteCommandHandler(
            new FakeRepository<Favorite>(new[] { favorite }), new FakeUnitOfWork());

        var result = await sut.Handle(NewRemoveCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(favorite.IsDeleted);
        Assert.Equal(result.Value, favorite.Id);
    }

    [Fact]
    public async Task Remove_ShouldFail_WhenNoActiveFavoriteExists()
    {
        var sut = new RemoveFavoriteCommandHandler(new FakeRepository<Favorite>(), new FakeUnitOfWork());

        var result = await sut.Handle(NewRemoveCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Remove_ShouldFail_WhenOnlySoftDeletedRecordExists()
    {
        // Ikinci silme denemesi NotFound donmeli (aktif kayit yok).
        var favorite = NewFavorite();
        favorite.Delete();
        favorite.ClearDomainEvents();

        var sut = new RemoveFavoriteCommandHandler(
            new FakeRepository<Favorite>(new[] { favorite }), new FakeUnitOfWork());

        var result = await sut.Handle(NewRemoveCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Remove_ShouldNotTouchFavoritesOfOtherBusinesses()
    {
        var target = NewFavorite();
        var other = NewFavorite(OtherBusinessId);

        var sut = new RemoveFavoriteCommandHandler(
            new FakeRepository<Favorite>(new[] { target, other }), new FakeUnitOfWork());

        var result = await sut.Handle(NewRemoveCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(target.IsDeleted);
        Assert.False(other.IsDeleted);
    }

    // ===== Uctan uca =====

    [Fact]
    public async Task AddRemoveAdd_ShouldWorkAsRoundTrip()
    {
        // Ekle -> cikar -> tekrar ekle zinciri kesintisiz calismali.
        var (addSut, favorites, _) = CreateAddSut();
        var removeSut = new RemoveFavoriteCommandHandler(favorites, new FakeUnitOfWork());

        var added = await addSut.Handle(NewAddCommand(), CancellationToken.None);
        var removed = await removeSut.Handle(NewRemoveCommand(), CancellationToken.None);
        var reAdded = await addSut.Handle(NewAddCommand(), CancellationToken.None);

        Assert.True(added.IsSuccess);
        Assert.True(removed.IsSuccess);
        Assert.True(reAdded.IsSuccess);
        Assert.NotEqual(added.Value, reAdded.Value);
        Assert.Equal(2, favorites.Items.Count);
        Assert.Single(favorites.Items.Where(f => !f.IsDeleted));
    }

    [Fact]
    public async Task Check_ShouldReportFalse_AfterRemove()
    {
        var (addSut, favorites, _) = CreateAddSut();
        var removeSut = new RemoveFavoriteCommandHandler(favorites, new FakeUnitOfWork());
        var checkSut = new CheckFavoriteQueryHandler(favorites);

        await addSut.Handle(NewAddCommand(), CancellationToken.None);
        var beforeRemove = await checkSut.Handle(NewCheckQuery(), CancellationToken.None);
        await removeSut.Handle(NewRemoveCommand(), CancellationToken.None);
        var afterRemove = await checkSut.Handle(NewCheckQuery(), CancellationToken.None);

        Assert.True(beforeRemove.Value);
        Assert.False(afterRemove.Value);
    }

    // ===== Yardimcilar =====

    private static (AddFavoriteCommandHandler Sut, FakeRepository<Favorite> Favorites, FakeUnitOfWork UnitOfWork)
        CreateAddSut()
    {
        var favorites = new FakeRepository<Favorite>();
        var unitOfWork = new FakeUnitOfWork();

        return (CreateAddHandler(favorites, unitOfWork), favorites, unitOfWork);
    }

    private static AddFavoriteCommandHandler CreateAddHandler(
        FakeRepository<Favorite> favorites,
        FakeUnitOfWork? unitOfWork = null)
        => new(
            favorites,
            new FakeRepository<User>(new[] { TestData.NewUser(UserId) }),
            new FakeRepository<Business>(new[]
            {
                TestData.NewBusiness(Guid.NewGuid(), BusinessId),
                TestData.NewBusiness(Guid.NewGuid(), OtherBusinessId)
            }),
            unitOfWork ?? new FakeUnitOfWork());

    private static AddFavoriteCommand NewAddCommand(Guid? businessId = null) => new()
    {
        UserId = UserId,
        BusinessId = businessId ?? BusinessId,
        Note = null
    };

    private static RemoveFavoriteCommand NewRemoveCommand() => new()
    {
        UserId = UserId,
        BusinessId = BusinessId
    };

    private static CheckFavoriteQuery NewCheckQuery() => new()
    {
        UserId = UserId,
        BusinessId = BusinessId
    };

    private static Favorite NewFavorite(Guid? businessId = null)
    {
        var favorite = new Favorite(UserId, businessId ?? BusinessId);
        favorite.ClearDomainEvents();
        return favorite;
    }
}
