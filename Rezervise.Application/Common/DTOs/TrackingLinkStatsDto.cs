using Rezervise.Domain.Enums;

namespace Rezervise.Application.Common.DTOs
{
    /// <summary>
    /// Tek bir takip linkinin ölçüm özeti.
    /// </summary>
    public class TrackingLinkStatsDto
    {
        public Guid LinkId { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>LinkOpened olaylarının toplam sayısı.</summary>
        public int TotalOpens { get; set; }

        /// <summary>Aynı linki açan farklı anonim oturum sayısı.</summary>
        public int UniqueSessions { get; set; }

        /// <summary>Olay tipi bazında kırılım.</summary>
        public List<TrackingEventCountDto> EventCounts { get; set; } = new();
    }

    /// <summary>Olay tipi kırılımındeki tek satır.</summary>
    public class TrackingEventCountDto
    {
        public EnumTrackingEventType EventType { get; set; }
        public int Count { get; set; }
    }
}
