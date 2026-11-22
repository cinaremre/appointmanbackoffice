using FluentValidation;

namespace Rezervise.Application.Features.Employees.Commands.RejectTimeOff
{
    public class RejectTimeOffCommandValidator : AbstractValidator<RejectTimeOffCommand>
    {
        public RejectTimeOffCommandValidator()
        {
            RuleFor(x => x.TimeOffId)
                .NotEmpty()
                .WithMessage("Izin ID bos olamaz.");

            RuleFor(x => x.RequesterId)
                .NotEmpty()
                .WithMessage("Istek sahibi ID bos olamaz.");
        }
    }
}
