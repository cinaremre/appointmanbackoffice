using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetPlatformOverview
{
    /// <summary>
    /// Platform geneli ozet raporu (dokuman Modul 18). Sayilar canli
    /// tablolardan hesaplanir; buyuk olcekte hazir ozet tablolarina
    /// tasinacaktir.
    /// </summary>
    public class GetPlatformOverviewQuery : IRequest<Result<PlatformOverviewDto>>
    {
    }

    /// <summary>Platform ozet metrikleri.</summary>
    public class PlatformOverviewDto
    {
        /// <summary>Silinmemis toplam isletme sayisi.</summary>
        public int TotalBusinesses { get; init; }

        /// <summary>Aktif isletme sayisi.</summary>
        public int ActiveBusinesses { get; init; }

        /// <summary>Askidaki (aktif olmayan) isletme sayisi.</summary>
        public int SuspendedBusinesses { get; init; }

        /// <summary>Son 30 gunde kayit olan isletme sayisi.</summary>
        public int NewRegistrations30Days { get; init; }

        public int TotalAppointments { get; init; }

        public int CompletedAppointments { get; init; }

        /// <summary>Iptal edilen randevu orani, yuzde.</summary>
        public decimal CancellationRate { get; init; }

        /// <summary>No-show randevu orani, yuzde.</summary>
        public decimal NoShowRate { get; init; }
    }
}
