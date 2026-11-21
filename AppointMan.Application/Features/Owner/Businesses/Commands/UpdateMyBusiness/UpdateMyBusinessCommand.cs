using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness
{
    /// <summary>
    /// Isletme sahibinin kendi isletme bilgilerini guncellemesi.
    /// Sahiplik handler'da dogrulanir; degisiklik entity'nin kendi
    /// Set metotlariyla yapilir ve denetim kaydina yazilir.
    /// </summary>
    public class UpdateMyBusinessCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yeni isletme adi; gonderildiyse guncellenir.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni aciklama; gonderildiyse guncellenir.</summary>
        public string? Description { get; set; }

        /// <summary>Opsiyonel islem gerekcesi; denetim kaydina yazilir.</summary>
        public string? Reason { get; set; }
    }
}
