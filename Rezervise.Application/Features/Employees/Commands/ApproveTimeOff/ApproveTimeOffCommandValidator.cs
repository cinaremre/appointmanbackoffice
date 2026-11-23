using FluentValidation;

namespace Rezervise.Application.Features.Employees.Commands.ApproveTimeOff
{
    public class ApproveTimeOffCommandValidator : AbstractValidator<ApproveTimeOffCommand>
    {
        public ApproveTimeOffCommandValidator()
        {
            RuleFor(x => x.TimeOffId)
                .NotEmpty()
                .WithMessage("İzin ID boş olamaz.");

            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("İstek sahibi ID boş olamaz.");
        }
    }
}
