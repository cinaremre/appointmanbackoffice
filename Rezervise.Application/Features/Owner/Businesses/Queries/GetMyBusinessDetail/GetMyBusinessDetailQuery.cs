using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinessDetail
{
    /// <summary>
    /// Isletme sahibinin kendi isletmesinin detayi. Sahiplik handler'da
    /// dogrulanir; baskasinin isletmesi FORBIDDEN doner.
    /// </summary>
    public class GetMyBusinessDetailQuery : IRequest<Result<OwnerBusinessDetailDto>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>Isletme detayi + operasyonel sayilar.</summary>
    public class OwnerBusinessDetailDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public bool IsActive { get; init; }

        public bool IsVerified { get; init; }

        public decimal? Rating { get; init; }

        public int TotalReviews { get; init; }

        public int CityId { get; init; }

        public string Address { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }

        /// <summary>Aktif (satista) hizmet sayisi.</summary>
        public int ActiveServiceCount { get; init; }

        /// <summary>Silinmemis calisan sayisi.</summary>
        public int EmployeeCount { get; init; }

        /// <summary>Bugunun (UTC) randevu sayisi.</summary>
        public int TodayAppointmentCount { get; init; }
    }
}
