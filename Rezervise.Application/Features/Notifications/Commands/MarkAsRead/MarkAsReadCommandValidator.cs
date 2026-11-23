using FluentValidation;

namespace Rezervise.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommandValidator : AbstractValidator<MarkAsReadCommand>
    {
        public MarkAsReadCommandValidator()
        {
            RuleFor(x => x.NotificationId)
                .NotEmpty()
                .WithMessage("Bildirim ID boş olamaz.");
        }
    }
}