using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessDetail
{
    /// <summary>
    /// Isletme detay sorgusu (dokuman Modul 8). Temel kayit bilgilerine ek
    /// olarak calisan, aktif hizmet ve son 30 gunun randevu sayilarini donerur.
    /// </summary>
    public class GetBusinessDetailQuery : IRequest<Result<AdminBusinessDetailDto>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Isletme detayi + operasyonel ozet sayilari.</summary>
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

        public decimal? Rating { get; init; }

        public int TotalReviews { get; init; }

        public DateTime CreatedAt { get; init; }

        /// <summary>Silinmemis calisan sayisi.</summary>
        public int EmployeeCount { get; init; }

        /// <summary>Aktif ve silinmemis hizmet sayisi.</summary>
        public int ActiveServiceCount { get; init; }

        /// <summary>Son 30 gunde olusan randevu sayisi.</summary>
        public int RecentAppointmentCount { get; init; }
    }
}
