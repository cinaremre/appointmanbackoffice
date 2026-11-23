using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.SetCashRegisterActive
{
    /// <summary>
    /// Kasayı aktif/pasif yapar (toggle). Deaktif kasa üzerinde yeni
    /// oturum açılamaz; mevcut açık oturum kapatılmadan değiştirilmez.
    /// </summary>
    public class SetCashRegisterActiveCommand : IRequest<Result<Guid>>
    {
        public Guid CashRegisterId { get; set; }

        /// <summary>true = aktif, false = deaktif.</summary>
        public bool IsActive { get; set; }
    }
}
