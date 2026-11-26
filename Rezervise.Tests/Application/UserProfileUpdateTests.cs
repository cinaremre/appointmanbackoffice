using Rezervise.Application.Features.Users.Commands.UpdateUserProfile;
using Rezervise.Application.Features.Users.Queries.GetUserById;
using Rezervise.Domain.Entities;
using Rezervise.Tests.Fakes;
using FluentValidation.TestHelper;

namespace Rezervise.Tests.Application;

/// <summary>
/// PUT /api/User/profile: gövdedeki email kaydedilmeli; GET /api/User/{id}
/// kayıtlı cityId / districtId döndürmelidir.
/// </summary>
public class UserProfileUpdateTests
{
    [Fact]
    public async Task UpdateProfile_ShouldSaveEmail_AndResetVerification()
    {
        var user = NewUser();
        user.VerifyEmail();

        var (sut, repository) = CreateUpdateSut(user);

        var result = await sut.Handle(new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "Yeni",
            LastName = "Isim",
            Email = "Yeni.Adres@Example.com"
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var saved = repository.Items.Single(u => u.Id == user.Id);
        Assert.Equal("yeni.adres@example.com", saved.Email);
        // E-posta değişince doğrulama durumu sıfırlanır.
        Assert.False(saved.IsEmailVerified);
    }

    [Fact]
    public async Task UpdateProfile_ShouldKeepEmail_WhenEmailIsEmpty()
    {
        var user = NewUser();
        var originalEmail = user.Email;

        var (sut, repository) = CreateUpdateSut(user);

        var result = await sut.Handle(new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "Yeni",
            LastName = "Isim",
            Email = "   "
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalEmail, repository.Items.Single(u => u.Id == user.Id).Email);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnConflict_WhenEmailBelongsToAnotherUser()
    {
        var user = NewUser();
        var other = NewUser();

        var (sut, _) = CreateUpdateSut(user, other);

        var result = await sut.Handle(new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "Yeni",
            LastName = "Isim",
            Email = other.Email
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("CONFLICT", result.Error!.Code);
        Assert.Equal("User.EmailTaken", result.Error.Message);
    }

    [Fact]
    public async Task Validator_ShouldRejectInvalidEmail_AndAcceptEmpty()
    {
        var sut = new UpdateUserProfileCommandValidator();

        var invalid = await sut.TestValidateAsync(new UpdateUserProfileCommand
        {
            UserId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Kullanici",
            Email = "gecersiz"
        });

        invalid.ShouldHaveValidationErrorFor(x => x.Email);

        var empty = await sut.TestValidateAsync(new UpdateUserProfileCommand
        {
            UserId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Kullanici",
            Email = null
        });

        empty.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task UpdateProfile_ShouldSaveCityAndDistrict_SoNextReadReturnsThem()
    {
        var user = NewUser();

        var repository = new FakeRepository<User>(new[] { user });
        var updateSut = new UpdateUserProfileCommandHandler(repository, new FakeUnitOfWork());

        var updateResult = await updateSut.Handle(new UpdateUserProfileCommand
        {
            UserId = user.Id,
            FirstName = "Yeni",
            LastName = "Isim",
            CityId = 34,
            DistrictId = 438
        }, CancellationToken.None);

        Assert.True(updateResult.IsSuccess);

        // Ayni kullanici üzerinden okuma: kaydedilen konum geri dönmeli.
        var readSut = new GetUserByIdQueryHandler(repository);
        var readResult = await readSut.Handle(
            new GetUserByIdQuery { UserId = user.Id }, CancellationToken.None);

        Assert.True(readResult.IsSuccess);
        Assert.Equal(34, readResult.Value.CityId);
        Assert.Equal(438, readResult.Value.DistrictId);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnCityAndDistrict()
    {
        var user = NewUser();
        user.UpdateLocation(34, 438);

        var sut = new GetUserByIdQueryHandler(new FakeRepository<User>(new[] { user }));

        var result = await sut.Handle(new GetUserByIdQuery { UserId = user.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(34, result.Value.CityId);
        Assert.Equal(438, result.Value.DistrictId);
    }

    private static User NewUser()
    {
        var user = TestData.NewUser(Guid.NewGuid());
        user.ClearDomainEvents();
        return user;
    }

    private static (UpdateUserProfileCommandHandler Sut, FakeRepository<User> Repository) CreateUpdateSut(
        User user,
        params User[] others)
    {
        var repository = new FakeRepository<User>(new[] { user }.Concat(others));
        return (new UpdateUserProfileCommandHandler(repository, new FakeUnitOfWork()), repository);
    }
}
