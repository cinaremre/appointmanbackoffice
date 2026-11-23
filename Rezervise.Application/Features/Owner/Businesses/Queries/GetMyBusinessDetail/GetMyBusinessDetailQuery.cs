using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinessDetail
{
    /// <summary>
    /// İşletme sahibinin kendi işletmesinin detayı. Sahiplik handler'da
    /// doğrulanır; başkasının işletmesi FORBIDDEN döner.
    /// </summary>
    public class GetMyBusinessDetailQuery : IRequest<Result<OwnerBusinessDetailDto>>
    {
        public Guid BusinessId { get; set; }
    }

    /// <summary>İşletme detayı + operasyonel sayılar.</summary>
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

        /// <summary>Aktif (satışta) hizmet sayısı.</summary>
        public int ActiveServiceCount { get; init; }

        /// <summary>Silinmemiş çalışan sayısı.</summary>
        public int EmployeeCount { get; init; }

        /// <summary>Bugünün (UTC) randevu sayısı.</summary>
        public int TodayAppointmentCount { get; init; }
    }
}
