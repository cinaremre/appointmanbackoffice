using FluentValidation;

namespace AppointMan.Application.Features.Employees.Commands.CreateEmployee
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Calisan adi bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Calisan adi en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Calisan adi en fazla 50 karakter olmalidir.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Calisan soyadi bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Calisan soyadi en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Calisan soyadi en fazla 50 karakter olmalidir.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarasi bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID bos olamaz.");

            RuleFor(x => x.Title)
                .MaximumLength(100)
                .WithMessage("Unvan en fazla 100 karakter olmalidir.");

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage("Biyografi en fazla 500 karakter olmalidir.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Gecersiz email formati.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Siralama numarasi negatif olamaz.");
        }
    }
}