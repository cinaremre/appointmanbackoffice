using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Randevunun pazarlama atfi. Bir randevu için tek atıf kaydı vardır;
    /// çift atıf DB seviyesinde de engellenir.
    /// </summary>
    public class AppointmentAttributionConfiguration : BaseConfiguration<AppointmentAttribution>
    {
        public override void Configure(EntityTypeBuilder<AppointmentAttribution> builder)
        {
            base.Configure(builder);

            builder.ToTable("AppointmentAttributions");

            builder.Property(x => x.AppointmentId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CampaignId)
                .IsRequired(false);

            builder.Property(x => x.TrackingLinkId)
                .IsRequired(false);

            builder.Property(x => x.FirstTouchSource)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.LastTouchSource)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.AttributionModel)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.AttributedAt)
                .IsRequired();

            builder.HasIndex(x => x.AppointmentId)
                .IsUnique()
                .HasDatabaseName("UX_AppointmentAttributions_AppointmentId");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_AppointmentAttributions_BusinessId");

            builder.HasIndex(x => x.CampaignId)
                .HasDatabaseName("IX_AppointmentAttributions_CampaignId");
        }
    }
}
