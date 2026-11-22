using Rezervise.Application.Common.Results;
using Rezervise.Domain.Enums;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs
{
    /// <summary>
    /// Veri aktarim isi listesi (dokuman Modul 20). Platform genelinde tum
    /// isler listelenir; olusturan kullanici (IsActedBy) bazli filtre bilincli
    /// olarak uygulanmaz, yonetici tum isleri gorur.
    /// </summary>
    public class GetAdminJobsQuery : IRequest<Result<AdminJobListDto>>
    {
        /// <summary>Yalnizca bu isletmenin isleri.</summary>
        public Guid? BusinessId { get; set; }

        /// <summary>Durum filtresi (EnumImportStatus).</summary>
        public EnumImportStatus? Status { get; set; }

        /// <summary>1 tabanli sayfa numarasi; kirpanma kurali AdminPagination'dadir.</summary>
        public int Page { get; set; }

        /// <summary>Sayfa boyutu; en fazla 200, varsayilan 50.</summary>
        public int PageSize { get; set; }
    }
}
