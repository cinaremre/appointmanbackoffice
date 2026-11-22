using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Anonim takip olayi. Append-only defterdir; kisisel veri icermez.
    /// </summary>
    public class TrackingEventConfiguration : BaseConfiguration<TrackingEvent>
    {
        public override void Configure(EntityTypeBuilder<TrackingEvent> builder)
        {
            base.Configure(builder);

            builder.ToTable("TrackingEvents");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.TrackingLinkId)
                .IsRequired(false);

            builder.Property(x => x.AnonymousSessionId)
                .IsRequired(false);

            builder.Property(x => x.EventType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.AppointmentId)
                .IsRequired(false);

            builder.Property(x => x.OccurredAt)
                .IsRequired();

            builder.HasIndex(x => new { x.BusinessId, x.OccurredAt })
                .HasDatabaseName("IX_TrackingEvents_BusinessId_OccurredAt");

            builder.HasIndex(x => x.TrackingLinkId)
                .HasDatabaseName("IX_TrackingEvents_TrackingLinkId");
        }
    }
}
