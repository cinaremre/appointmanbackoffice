using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Isletme stok urunu. Stok bakiyesi Product uzerinde tutulmaz;
    /// StockMovement toplamlarindan hesaplanir (defter mantigi).
    /// </summary>
    public class ProductConfiguration : BaseConfiguration<Product>
    {
        public override void Configure(EntityTypeBuilder<Product> builder)
        {
            base.Configure(builder);

            builder.ToTable("Products");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Sku)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Barcode)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CategoryId)
                .IsRequired(false);

            builder.Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.PurchasePrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.SalePrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.MinimumStock)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // SKU benzersizligi isletme bazindadir.
            builder.HasIndex(x => new { x.BusinessId, x.Sku })
                .IsUnique()
                .HasDatabaseName("UX_Products_BusinessId_Sku");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_Products_BusinessId");
        }
    }
}
