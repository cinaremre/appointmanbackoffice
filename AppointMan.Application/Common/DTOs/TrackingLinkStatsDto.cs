using AppointMan.Domain.Enums;

namespace AppointMan.Application.Common.DTOs
{
    /// <summary>
    /// Tek bir takip linkinin olcum ozeti.
    /// </summary>
    public class TrackingLinkStatsDto
    {
        public Guid LinkId { get; set; }
        public string TrackingCode { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;

        /// <summary>LinkOpened olaylarinin toplam sayisi.</summary>
        public int TotalOpens { get; set; }

        /// <summary>Ayni linki acan farkli anonim oturum sayisi.</summary>
        public int UniqueSessions { get; set; }

        /// <summary>Olay tipi bazinda kirilim.</summary>
        public List<TrackingEventCountDto> EventCounts { get; set; } = new();
    }

    /// <summary>Olay tipi kirilimindeki tek satir.</summary>
    public class TrackingEventCountDto
    {
        public EnumTrackingEventType EventType { get; set; }
        public int Count { get; set; }
    }
}
