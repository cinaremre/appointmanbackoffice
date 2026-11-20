using AppointMan.Application.Features.Users.Commands.RegisterUser;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;
using FluentValidation.TestHelper;

namespace AppointMan.Tests.Application;

/// <summary>
/// Kayit dogrulama mesajlari: yas kurali ihlalinde generic "beklenmeyen hata"
/// yerine anlamli lokalizasyon anahtari donmelidir.
/// </summary>
public class RegisterUserDateOfBirthValidationTests
{
    [Fact]
    public void Validator_ShouldReturnMinimumAgeKey_WhenUserIsUnder18()
    {
        var sut = new RegisterUserCommandValidator(new StubLocalizationService());
        var command = NewCommand(DateTime.UtcNow.AddYears(-17));

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Validation.DateOfBirthMinAge");
    }

    [Fact]
    public void Validator_ShouldReturnKeyNotGenericMessage()
    {
        var sut = new RegisterUserCommandValidator(new StubLocalizationService());
        var command = NewCommand(DateTime.UtcNow.AddYears(-10));

        var result = sut.TestValidate(command);

        // Generic mesaja dusmemeli: anahtar donmeli.
        Assert.DoesNotContain(
            result.Errors,
            e => e.PropertyName == nameof(command.DateOfBirth) && e.ErrorMessage == "Error.Unexpected");
    }

    [Fact]
    public void Validator_ShouldAcceptDateOfBirth_WhenUserIsAdult()
    {
        var sut = new RegisterUserCommandValidator(new StubLocalizationService());
        var command = NewCommand(DateTime.UtcNow.AddYears(-25));

        var result = sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validator_ShouldRequirePhoneNumber()
    {
        var sut = new RegisterUserCommandValidator(new StubLocalizationService());
        var command = NewCommand(DateTime.UtcNow.AddYears(-25));
        command.PhoneNumber = string.Empty;

        var result = sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("Validation.PhoneNumberRequired");
    }

    private static RegisterUserCommand NewCommand(DateTime dateOfBirth) => new()
    {
        FirstName = "Test",
        LastName = "Kullanici",
        Email = "test@example.com",
        PhoneNumber = "+905551112233",
        Password = "Passw0rd!23",
        Role = EnumUserRole.Customer,
        DateOfBirth = dateOfBirth
    };
}
