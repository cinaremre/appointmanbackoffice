using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetPlatformOverview
{
    /// <summary>
    /// Platform geneli özet raporu (doküman Modül 18). Sayılar canlı
    /// tablolardan hesaplanır; büyük ölçekte hazır özet tablolarına
    /// taşınacaktır.
    /// </summary>
    public class GetPlatformOverviewQuery : IRequest<Result<PlatformOverviewDto>>
    {
    }

    /// <summary>Platform özet metrikleri.</summary>
    public class PlatformOverviewDto
    {
        /// <summary>Silinmemiş toplam işletme sayısı.</summary>
        public int TotalBusinesses { get; init; }

        /// <summary>Aktif işletme sayısı.</summary>
        public int ActiveBusinesses { get; init; }

        /// <summary>Askıdaki (aktif olmayan) işletme sayısı.</summary>
        public int SuspendedBusinesses { get; init; }

        /// <summary>Son 30 günde kayıt olan işletme sayısı.</summary>
        public int NewRegistrations30Days { get; init; }

        public int TotalAppointments { get; init; }

        public int CompletedAppointments { get; init; }

        /// <summary>İptal edilen randevu oranı, yüzde.</summary>
        public decimal CancellationRate { get; init; }

        /// <summary>No-show randevu oranı, yüzde.</summary>
        public decimal NoShowRate { get; init; }
    }
}
