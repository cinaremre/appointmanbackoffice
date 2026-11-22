using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.SetCashRegisterActive
{
    /// <summary>
    /// Kasayi aktif/pasif yapar (toggle). Deaktif kasa uzerinde yeni
    /// oturum acilamaz; mevcut acik oturum kapatilmadan degistirilmez.
    /// </summary>
    public class SetCashRegisterActiveCommand : IRequest<Result<Guid>>
    {
        public Guid CashRegisterId { get; set; }

        /// <summary>true = aktif, false = deaktif.</summary>
        public bool IsActive { get; set; }
    }
}
