using FluentValidation;

namespace Rezervise.Application.Features.Users.Commands.UpdateUserProfile
{
    public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanıcı ID boş olamaz.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("İsim boş olamaz.")
                .MinimumLength(2)
                .WithMessage("İsim en az 2 karakter olmalıdır.")
                .MaximumLength(50)
                .WithMessage("İsim en fazla 50 karakter olmalıdır.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Soyisim boş olamaz.")
                .MinimumLength(2)
                .WithMessage("Soyisim en az 2 karakter olmalıdır.")
                .MaximumLength(50)
                .WithMessage("Soyisim en fazla 50 karakter olmalıdır.");

            RuleFor(x => x.DateOfBirth)
                .Must(date => !date.HasValue || date.Value < DateTime.UtcNow.AddYears(-18))
                .WithMessage("Yaş 18'den büyük olmalıdır.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Geçersiz e-posta adresi.")
                .MaximumLength(255)
                .WithMessage("E-posta en fazla 255 karakter olabilir.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.CityId)
                .GreaterThan(0)
                .When(x => x.CityId.HasValue)
                .WithMessage("İl ID geçersiz.");

            RuleFor(x => x.DistrictId)
                .GreaterThan(0)
                .When(x => x.DistrictId.HasValue)
                .WithMessage("İlçe ID geçersiz.");
        }
    }
}