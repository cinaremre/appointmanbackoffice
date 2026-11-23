using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Stok hareketi defteri. Kayıtlar değiştirilemez; bakiye hareket
    /// toplamlarından hesaplanır. Aynı (ReferenceType, ReferenceId)
    // çifti işlenemez (çift işleme engeli).
    /// </summary>
    public class StockMovementConfiguration : BaseConfiguration<StockMovement>
    {
        public override void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            base.Configure(builder);

            builder.ToTable("StockMovements");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.ProductId)
                .IsRequired();

            builder.Property(x => x.MovementType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Quantity)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.UnitCost)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.ReferenceType)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.ReferenceId)
                .IsRequired(false);

            builder.Property(x => x.OccurredAt)
                .IsRequired();

            builder.HasIndex(x => x.ProductId)
                .HasDatabaseName("IX_StockMovements_ProductId");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_StockMovements_BusinessId");

            // Çift işleme engeli: aynı referanslı hareket ikinci kez
            // yazılamaz. Referanssız (manuel) hareketler filtreye girmez.
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId })
                .IsUnique()
                .HasFilter("[ReferenceType] IS NOT NULL AND [ReferenceId] IS NOT NULL")
                .HasDatabaseName("UX_StockMovements_Reference");
        }
    }
}
