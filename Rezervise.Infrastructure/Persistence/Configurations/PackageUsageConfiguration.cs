using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Paket hak hareketi. Aynı randevu için birden fazla hareket olabileceği
    /// (Reserve -> Consume geçişi gibi) için AppointmentId üzerinde unique
    /// kısıt YOKTUR; çift rezervasyon engeli handler tarafından Active
    /// Reservation sorgusuyla sağlanır.
    /// </summary>
    public class PackageUsageConfiguration : BaseConfiguration<PackageUsage>
    {
        public override void Configure(EntityTypeBuilder<PackageUsage> builder)
        {
            base.Configure(builder);

            builder.ToTable("PackageUsages");

            builder.Property(x => x.CustomerPackageId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.AppointmentId)
                .IsRequired();

            builder.Property(x => x.UsageType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(x => x.CustomerPackageId)
                .HasDatabaseName("IX_PackageUsages_CustomerPackageId");

            builder.HasIndex(x => x.AppointmentId)
                .HasDatabaseName("IX_PackageUsages_AppointmentId");
        }
    }
}
