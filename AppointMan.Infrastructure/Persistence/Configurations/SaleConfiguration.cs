using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class SaleConfiguration : BaseConfiguration<Sale>
    {
        public override void Configure(EntityTypeBuilder<Sale> builder)
        {
            base.Configure(builder);

            builder.ToTable("Sales");

            builder.Property(x => x.Subtotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.DiscountTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TaxTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.GrandTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("TRY");

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumSaleStatus.Pending);

            builder.Property(x => x.SoldAt)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.AppointmentId)
                .IsRequired(false);

            builder.Property(x => x.CustomerId)
                .IsRequired(false);

            builder.HasMany(x => x.Lines)
                .WithOne(x => x.Sale)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.AppointmentId);
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);

            // Gunluk satis ozeti sorgusu (isletme + gun) icin birlesik index.
            builder.HasIndex(x => new { x.BusinessId, x.SoldAt })
                .HasDatabaseName("IX_Sales_BusinessId_SoldAt");
        }
    }
}
