using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.UpdateBusinessByAdmin
{
    /// <summary>
    /// Platform yöneticisinin işletme temel bilgilerini güncellemesi
    /// (doküman Modül 8). Yalnızca boş olmayan alanlar güncellenir;
    /// işlem denetim kaydına yazılır.
    /// </summary>
    public class UpdateBusinessByAdminCommand : IRequest<Result<Guid>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Yeni işletme adı; boş ise değiştirilmez.</summary>
        public string? Name { get; set; }

        /// <summary>Yeni açıklama; null ise değiştirilmez.</summary>
        public string? Description { get; set; }

        /// <summary>Yöneticinin işlem gerekçesi; denetim kaydına yazılır.</summary>
        public string? Reason { get; set; }
    }
}
