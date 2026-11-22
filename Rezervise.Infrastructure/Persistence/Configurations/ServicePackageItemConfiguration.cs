using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Paketin icindeki tek hizmet kalemi. Ayni pakette ayni hizmet bir kez
    /// tanimlanabilir.
    /// </summary>
    public class ServicePackageItemConfiguration : BaseConfiguration<ServicePackageItem>
    {
        public override void Configure(EntityTypeBuilder<ServicePackageItem> builder)
        {
            base.Configure(builder);

            builder.ToTable("ServicePackageItems");

            builder.Property(x => x.ServicePackageId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.IncludedSessions)
                .IsRequired();

            builder.HasIndex(x => new { x.ServicePackageId, x.ServiceId })
                .IsUnique()
                .HasDatabaseName("UX_ServicePackageItems_Package_Service");
        }
    }
}
