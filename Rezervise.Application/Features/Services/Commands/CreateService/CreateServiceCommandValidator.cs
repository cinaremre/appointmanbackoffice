using FluentValidation;

namespace Rezervise.Application.Features.Services.Commands.CreateService
{
    public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
    {
        public CreateServiceCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Hizmet adı boş olamaz.")
                .MinimumLength(2)
                .WithMessage("Hizmet adı en az 2 karakter olmalıdır.")
                .MaximumLength(100)
                .WithMessage("Hizmet adı en fazla 100 karakter olmalıdır.");

            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Fiyat 0'dan büyük olmalıdır.");

            RuleFor(x => x.Currency)
                .NotEmpty()
                .WithMessage("Para birimi boş olamaz.")
                .Length(3)
                .WithMessage("Para birimi 3 karakter olmalıdır.");

            RuleFor(x => x.DurationInMinutes)
                .InclusiveBetween(5, 480)
                .WithMessage("Süre 5-480 dakika arasında olmalıdır.")
                .Must(d => d % 5 == 0)
                .WithMessage("Süre 5 dakikanın katı olmalıdır.");

            RuleFor(x => x.ServiceCategoryId)
                .NotEmpty()
                .WithMessage("Kategori ID boş olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("İşletme ID boş olamaz.");

            RuleFor(x => x.OwnerId)
                .NotEmpty()
                .WithMessage("Sahip ID boş olamaz.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Açıklama en fazla 500 karakter olmalıdır.");
        }
    }
}