using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Admin.Services.Queries.PreviewBulkServiceOperation
{
    /// <summary>
    /// Toplu hizmet islemi icin BulkOperation kaydi olusturur (dokuman Modul 10).
    /// DryRun=true (onizleme) kaydi etkilenecek kayit sayisini ve gecersiz
    /// ID'leri raporlar ve yurutulemez. DryRun=false kaydi gercek islem icin
    /// Validating durumunda birakilir ve execute endpoint'i ile yurutulur.
    /// </summary>
    public class PreviewBulkServiceOperationQuery : IRequest<Result<BulkOperationDto>>
    {
        public EnumBulkOperationType OperationType { get; set; }

        /// <summary>Etkilenmek istenen hizmet ID'leri.</summary>
        public List<Guid> ServiceIds { get; set; } = new();

        /// <summary>ChangeServiceCategory icin hedef kategori; diger tiplerde bos.</summary>
        public Guid? TargetCategoryId { get; set; }

        /// <summary>Isletme filtresi; platform genelinde null birakilir.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>
        /// True (varsayilan) = onizleme kaydi, yurutulemez.
        /// False = gercek islem kaydi, execute ile yurutulur.
        /// </summary>
        public bool DryRun { get; set; } = true;
    }

    /// <summary>Onizleme/olusturma cevabi; InvalidIds istemciye duzeltilmesi icin doner.</summary>
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
    /// BulkOperation.RequestedPayload icin JSON model. Append-only: kayit
    /// olusturulduktan sonra payload degismez, yurutme bu modelden okur.
    /// </summary>
    public class BulkServiceOperationPayload
    {
        public List<Guid> ServiceIds { get; set; } = new();
        public Guid? TargetCategoryId { get; set; }
    }
}
