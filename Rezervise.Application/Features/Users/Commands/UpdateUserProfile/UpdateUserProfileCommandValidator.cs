using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Isim bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Isim en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Isim en fazla 50 karakter olmalidir.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Soyisim bos olamaz.")
                .MinimumLength(2)
                .WithMessage("Soyisim en az 2 karakter olmalidir.")
                .MaximumLength(50)
                .WithMessage("Soyisim en fazla 50 karakter olmalidir.");

            RuleFor(x => x.DateOfBirth)
                .Must(date => !date.HasValue || date.Value < DateTime.UtcNow.AddYears(-18))
                .WithMessage("Yas 18'den büyük olmalidir.");

            RuleFor(x => x.CityId)
                .GreaterThan(0)
                .When(x => x.CityId.HasValue)
                .WithMessage("Il ID gecersiz.");

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .When(x => x.DistrictId.HasValue)
                .WithMessage("Ilce ID gecersiz.");
        }
    }
}