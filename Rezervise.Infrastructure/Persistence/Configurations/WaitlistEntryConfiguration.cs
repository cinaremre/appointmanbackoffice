using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Bekleme listesi kaydi. Ayni musteri + hizmet + Active kombinasyonu
    /// tek kayda sinirlidir (cift kayit engeli).
    /// </summary>
    public class WaitlistEntryConfiguration : BaseConfiguration<WaitlistEntry>
    {
        public override void Configure(EntityTypeBuilder<WaitlistEntry> builder)
        {
            base.Configure(builder);

            builder.ToTable("WaitlistEntries");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.PreferredEmployeeId)
                .IsRequired(false);

            builder.Property(x => x.EarliestDate)
                .IsRequired();

            builder.Property(x => x.LatestDate)
                .IsRequired(false);

            builder.Property(x => x.PreferredTimeRange)
                .IsRequired(false)
                .HasMaxLength(20);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId, x.ServiceId })
                .HasDatabaseName("IX_WaitlistEntries_Business_Customer_Service");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_WaitlistEntries_Status");
        }
    }
}
