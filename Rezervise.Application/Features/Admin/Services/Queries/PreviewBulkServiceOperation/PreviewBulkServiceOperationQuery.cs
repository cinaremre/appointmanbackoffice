using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation
{
    /// <summary>
    /// Toplu hizmet işlemi için BulkOperation kaydı oluşturur (doküman Modül 10).
    /// DryRun=true (onizleme) kaydı etkilenecek kayıt sayısını ve geçersiz
    /// ID'leri raporlar ve yürütülemez. DryRun=false kaydı gerçek işlem için
    /// Validating durumunda bırakılır ve execute endpoint'i ile yürütülür.
    /// </summary>
    public class PreviewBulkServiceOperationQuery : IRequest<Result<BulkOperationDto>>
    {
        public EnumBulkOperationType OperationType { get; set; }

        /// <summary>Etkilenmek istenen hizmet ID'leri.</summary>
        public List<Guid> ServiceIds { get; set; } = new();

        /// <summary>ChangeServiceCategory için hedef kategori; diğer tiplerde boş.</summary>
        public Guid? TargetCategoryId { get; set; }

        /// <summary>İşletme filtresi; platform genelinde null bırakılır.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// True (varsayılan) = onizleme kaydı, yürütülemez.
        /// False = gerçek işlem kaydı, execute ile yürütülür.
        /// </summary>
        public bool DryRun { get; set; } = true;
    }

    /// <summary>Onizleme/oluşturma cevabı; InvalidIds istemciye düzeltilmesi için döner.</summary>
    public class BulkOperationDto
    {
        public Guid Id { get; set; }
        public EnumBulkOperationType OperationType { get; set; }
        public bool DryRun { get; set; }
        public EnumBulkOperationStatus Status { get; set; }
        public int TotalRecords { get; set; }
        public IReadOnlyList<Guid> InvalidIds { get; set; } = Array.Empty<Guid>();
    }

    /// <summary>
    /// BulkOperation.RequestedPayload için JSON model. Append-only: kayıt
    /// oluşturulduktan sonra payload değişmez, yürütme bu modelden okur.
    /// </summary>
    public class BulkServiceOperationPayload
    {
        public List<Guid> ServiceIds { get; set; } = new();
        public Guid? TargetCategoryId { get; set; }
    }
}
