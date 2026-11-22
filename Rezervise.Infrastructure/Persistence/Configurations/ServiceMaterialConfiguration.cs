using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Hizmet recetesi: hizmet basina tuketilen malzeme tanimi.
    /// Ayni (BusinessId, ServiceId, ProductId) cifti bir kez tanimlanabilir.
    /// </summary>
    public class ServiceMaterialConfiguration : BaseConfiguration<ServiceMaterial>
    {
        public override void Configure(EntityTypeBuilder<ServiceMaterial> builder)
        {
            base.Configure(builder);

            builder.ToTable("ServiceMaterials");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.ProductId)
                .IsRequired();

            builder.Property(x => x.QuantityPerService)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(x => new { x.BusinessId, x.ServiceId, x.ProductId })
                .IsUnique()
                .HasDatabaseName("UX_ServiceMaterials_Business_Service_Product");

            builder.HasIndex(x => x.ServiceId)
                .HasDatabaseName("IX_ServiceMaterials_ServiceId");
        }
    }
}
