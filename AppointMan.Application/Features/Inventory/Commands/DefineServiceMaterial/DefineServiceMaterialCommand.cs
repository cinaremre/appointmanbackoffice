using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Inventory.Commands.DefineServiceMaterial
{
    /// <summary>
    /// Hizmet recetesi tanimlar: bir hizmet gerceklestirildiginde
    /// tuketilecek malzemeyi ve miktari belirtir. Ayni (ServiceId, ProductId)
    /// cifti isletme bazinda bir kez tanimlanabilir.
    /// OwnerId govdeden degil JWT'den cozulur (Api katmani set eder).
    /// </summary>
    public class DefineServiceMaterialCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }
        public Guid OwnerId { get; set; }

        public Guid ServiceId { get; set; }

        public Guid ProductId { get; set; }

        /// <summary>Bir hizmet gerceklestirildiginde tuketilen miktar (pozitif).</summary>
        public decimal QuantityPerService { get; set; }

        /// <summary>Olcu birimi (varsayilan "adet").</summary>
        public string Unit { get; set; } = "adet";
    }
}
