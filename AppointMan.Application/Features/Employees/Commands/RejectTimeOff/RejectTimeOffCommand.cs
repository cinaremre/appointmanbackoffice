using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Employees.Commands.RejectTimeOff
{
    public class RejectTimeOffCommand : IRequest<Result<Guid>>
    {
        public Guid TimeOffId { get; set; }

        /// <summary>
        /// JWT'den cozulen istek sahibi. Handler govde degerine guvenmez;
        /// yalnizca token kimligiyle tutarli oldugunda kabul eder.
        /// </summary>
        public Guid RequesterId { get; set; }
    }
}
