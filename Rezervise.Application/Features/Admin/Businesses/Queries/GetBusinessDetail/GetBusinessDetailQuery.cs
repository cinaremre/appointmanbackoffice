using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessDetail
{
    /// <summary>
    /// İşletme detay sorgusu (doküman Modül 8). Temel kayıt bilgilerine ek
    /// olarak çalışan, aktif hizmet ve son 30 günün randevu sayılarını dönerur.
    /// </summary>
    public class GetBusinessDetailQuery : IRequest<Result<AdminBusinessDetailDto>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>İşletme detayı + operasyonel özet sayıları.</summary>
    public class AdminBusinessDetailDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        /// <summary>"Active" veya "Suspended".</summary>
        public string Status { get; init; } = string.Empty;

        public int CityId { get; init; }

        public string Address { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public Guid OwnerId { get; init; }

        public bool IsVerified { get; init; }

        /// <summary>Backoffice onayı: false ise işletme onay beklemektedir.</summary>
        public bool IsApproved { get; init; }

        public decimal? Rating { get; init; }

        public int TotalReviews { get; init; }

        public DateTime CreatedAt { get; init; }

        /// <summary>Silinmemiş çalışan sayısı.</summary>
        public int EmployeeCount { get; init; }

        /// <summary>Aktif ve silinmemiş hizmet sayısı.</summary>
        public int ActiveServiceCount { get; init; }

        /// <summary>Son 30 günde oluşan randevu sayısı.</summary>
        public int RecentAppointmentCount { get; init; }
    }
}
