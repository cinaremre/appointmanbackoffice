using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.MoveBusinessCategory
{
    /// <summary>
    /// Kategoriyi başka bir üst kategorinin altına veya kök seviyesine taşır
    /// (doküman 9.3). Hedef üst zincirinde kategorinin kendisi varsa döngü
    /// oluşur ve istek Conflict ile reddedilir.
    /// </summary>
    public class MoveBusinessCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }

        /// <summary>Hedef üst kategori; null ise kök seviyesine taşınır.</summary>
        public Guid? TargetParentId { get; set; }
    }
}
