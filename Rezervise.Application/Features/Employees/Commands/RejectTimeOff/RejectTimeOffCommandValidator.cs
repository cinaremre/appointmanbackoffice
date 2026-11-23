using FluentValidation;

namespace Rezervise.Application.Features.Employees.Commands.RejectTimeOff
{
    public class RejectTimeOffCommandValidator : AbstractValidator<RejectTimeOffCommand>
    {
        public RejectTimeOffCommandValidator()
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
