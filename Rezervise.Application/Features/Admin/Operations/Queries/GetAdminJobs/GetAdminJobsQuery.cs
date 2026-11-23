using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs
{
    /// <summary>
    /// Veri aktarım isi listesi (doküman Modül 20). Platform genelinde tüm
    /// işler listelenir; oluşturan kullanıcı (IsActedBy) bazlı filtre bilinçli
    /// olarak uygulanmaz, yönetici tüm işleri görür.
    /// </summary>
    public class GetAdminJobsQuery : IRequest<Result<AdminJobListDto>>
    {
        /// <summary>Yalnızca bu işletmenin işleri.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Durum filtresi (EnumImportStatus).</summary>
        public EnumImportStatus? Status { get; set; }

        /// <summary>1 tabanlı sayfa numarası; kırpanma kuralı AdminPagination'dadır.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayılan 50.</summary>
        public int PageSize { get; set; }
    }
}
