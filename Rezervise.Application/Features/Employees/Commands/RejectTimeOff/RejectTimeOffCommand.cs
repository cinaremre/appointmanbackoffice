using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Employees.Commands.RejectTimeOff
{
    public class RejectTimeOffCommand : IRequest<Result<Guid>>
    {
        public Guid TimeOffId { get; set; }

        /// <summary>
        /// JWT'den çözülen istek sahibi. Handler gövde değerine güvenmez;
        /// yalnızca token kimliğiyle tutarlı öldüğünda kabul eder.
        /// </summary>
        public Guid RequesterId { get; set; }
    }
}
