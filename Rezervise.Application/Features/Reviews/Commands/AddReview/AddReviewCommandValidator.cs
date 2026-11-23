using Rezervise.Application.Common.Interfaces;
using FluentValidation;

namespace Rezervise.Application.Features.Reviews.Commands.AddReview
{
    public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
    {
        public AddReviewCommandValidator(ILocalizationService localizer)
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.UserIdRequired"));

            RuleFor(x => x.BusinessId)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.BusinessIdRequired"));

            // Yorum yalnızca tamamlanmış bir randevuya bağlanabilir.
            RuleFor(x => x.AppointmentId)
                .NotEmpty()
                .WithMessage("Randevu ID zorunludur.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage(localizer.Get("Validation.RatingRange"));

            RuleFor(x => x.Comment)
                .NotEmpty()
                .WithMessage(localizer.Get("Validation.CommentRequired"))
                .MinimumLength(10)
                .WithMessage(localizer.Get("Validation.CommentMinLength"))
                .MaximumLength(500)
                .WithMessage(localizer.Get("Validation.CommentMaxLength"));
        }
    }
}
