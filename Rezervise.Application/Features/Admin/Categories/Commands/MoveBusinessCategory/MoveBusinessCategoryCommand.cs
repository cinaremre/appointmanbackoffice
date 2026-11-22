using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.MoveBusinessCategory
{
    /// <summary>
    /// Kategoriyi baska bir ust kategorinin altina veya kok seviyesine tasir
    /// (dokuman 9.3). Hedef ust zincirinde kategorinin kendisi varsa dongu
    /// olusur ve istek Conflict ile reddedilir.
    /// </summary>
    public class MoveBusinessCategoryCommand : IRequest<Result<Guid>>
    {
        public Guid CategoryId { get; set; }

        /// <summary>Hedef ust kategori; null ise kok seviyesine tasinir.</summary>
        public Guid? TargetParentId { get; set; }
    }
}
