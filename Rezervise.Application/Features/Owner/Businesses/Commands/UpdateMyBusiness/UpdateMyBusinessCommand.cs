using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness
{
    /// <summary>
    /// İşletme sahibinin kendi işletme bilgilerini güncellemesi.
    /// Sahiplik handler'da doğrulanır; değişiklik entity'nin kendi
    /// Set metotlarıyla yapılır ve denetim kaydına yazılır.
    /// </summary>
    public class UpdateMyBusinessCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yeni işletme adı; gönderildiyse güncellenir.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni açıklama; gönderildiyse güncellenir.</summary>
        public string? Description { get; set; }

        /// <summary>Opsiyonel işlem gerekçesi; denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
