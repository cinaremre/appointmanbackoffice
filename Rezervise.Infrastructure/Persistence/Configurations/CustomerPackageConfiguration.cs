using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Müşterinin satın aldığı paket örneği. Kalan hak entity'de tutulmaz;
    /// PackageUsage hareketlerinden hesaplanır.
    /// </summary>
    public class CustomerPackageConfiguration : BaseConfiguration<CustomerPackage>
    {
        public override void Configure(EntityTypeBuilder<CustomerPackage> builder)
        {
            base.Configure(builder);

            builder.ToTable("CustomerPackages");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.ServicePackageId)
                .IsRequired();

            builder.Property(x => x.PurchasedAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasOne(x => x.ServicePackage)
                .WithMany()
                .HasForeignKey(x => x.ServicePackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId })
                .HasDatabaseName("IX_CustomerPackages_Business_Customer");

            builder.HasIndex(x => x.ExpiresAt)
                .HasDatabaseName("IX_CustomerPackages_ExpiresAt");
        }
    }
}
