using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Paket hak hareketi. Ayni randevu icin birden fazla hareket olabilecegi
    /// (Reserve -> Consume gecisi gibi) icin AppointmentId uzerinde unique
    /// kisit YOKTUR; cift rezervasyon engeli handler tarafindan Active
    /// Reservation sorgusuyla saglanir.
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
