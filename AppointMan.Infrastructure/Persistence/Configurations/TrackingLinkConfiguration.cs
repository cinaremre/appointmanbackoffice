using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Paylasilabilir takip linki (QR / WhatsApp / Instagram).
    /// TrackingCode benzersiz kisa koddur; anonim olay kayitlari bu kod ile eslesir.
    /// </summary>
    public class TrackingLinkConfiguration : BaseConfiguration<TrackingLink>
    {
        public override void Configure(EntityTypeBuilder<TrackingLink> builder)
        {
            base.Configure(builder);

            builder.ToTable("TrackingLinks");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.TargetType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.TargetId)
                .IsRequired(false);

            builder.Property(x => x.Source)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Medium)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.Campaign)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.TrackingCode)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.DestinationPath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.TrackingCode)
                .IsUnique()
                .HasDatabaseName("UX_TrackingLinks_TrackingCode");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_TrackingLinks_BusinessId");
        }
    }
}
