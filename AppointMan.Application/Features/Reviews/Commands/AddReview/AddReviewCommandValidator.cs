using FluentValidation;

namespace AppointMan.Application.Features.Reviews.Commands.AddReview
{
    public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
    {
        public AddReviewCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("Kullanici ID bos olamaz.");

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage("Isletme ID bos olamaz.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Puan 1-5 arasinda olmalidir.");

            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage("Yorum bos olamaz.")
                .MinimumLength(10)
                .WithMessage("Yorum en az 10 karakter olmalidir.")
                .MaximumLength(500)
                .WithMessage("Yorum en fazla 500 karakter olmalidir.");
        }
    }
}