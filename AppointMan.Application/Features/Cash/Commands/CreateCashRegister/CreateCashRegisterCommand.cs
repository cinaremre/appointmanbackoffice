using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Cash.Commands.CreateCashRegister
{
    /// <summary>
    /// Isletmeye yeni kasa tanimlar.
    /// </summary>
    public class CreateCashRegisterCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kasa adi (2-100 karakter).</summary>
        public string Name { get; set; } = string.Empty;
    }
}
