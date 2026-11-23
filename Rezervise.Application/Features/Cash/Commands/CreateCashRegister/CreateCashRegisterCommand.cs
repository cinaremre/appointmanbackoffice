using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.CreateCashRegister
{
    /// <summary>
    /// İşletmeye yeni kasa tanımlar.
    /// </summary>
    public class CreateCashRegisterCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Kasa adı (2-100 karakter).</summary>
        public string Name { get; set; } = string.Empty;
    }
}
