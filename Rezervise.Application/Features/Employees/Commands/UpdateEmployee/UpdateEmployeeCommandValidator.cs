using FluentValidation;

namespace Rezervise.Application.Features.Employees.Commands.UpdateEmployee
{
    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty()
                .WithMessage("Çalışan ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID boş olamaz.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Çalışan adı boş olamaz.")
                .MinimumLength(2)
                .WithMessage("Çalışan adı en az 2 karakter olmalıdır.")
                .MaximumLength(50)
                .WithMessage("Çalışan adı en fazla 50 karakter olmalıdır.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Çalışan soyadı boş olamaz.")
                .MinimumLength(2)
                .WithMessage("Çalışan soyadı en az 2 karakter olmalıdır.")
                .MaximumLength(50)
                .WithMessage("Çalışan soyadı en fazla 50 karakter olmalıdır.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Telefon numarası boş olamaz.");

            RuleFor(x => x.Title)
                .MaximumLength(100)
                .WithMessage("Unvan en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage("Biyografi en fazla 500 karakter olmalıdır.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Geçersiz email formatı.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Sıralama numarası negatif olamaz.");
        }
    }
}