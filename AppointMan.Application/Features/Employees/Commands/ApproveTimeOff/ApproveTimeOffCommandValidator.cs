using FluentValidation;

namespace AppointMan.Application.Features.Employees.Commands.ApproveTimeOff
{
    public class ApproveTimeOffCommandValidator : AbstractValidator<ApproveTimeOffCommand>
    {
        public ApproveTimeOffCommandValidator()
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
