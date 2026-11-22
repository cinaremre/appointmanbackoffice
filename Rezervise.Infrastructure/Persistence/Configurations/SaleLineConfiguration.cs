using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class SaleLineConfiguration : BaseConfiguration<SaleLine>
    {
        public override void Configure(EntityTypeBuilder<SaleLine> builder)
        {
            base.Configure(builder);

            builder.ToTable("SaleLines");

            builder.Property(x => x.ItemType)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumSaleItemType.Service);

            builder.Property(x => x.ServiceId)
                .IsRequired(false);

            builder.Property(x => x.ProductId)
                .IsRequired(false);

            builder.Property(x => x.Quantity)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.DiscountAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.TaxAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.LineTotal)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.SaleId)
                .IsRequired();

            builder.HasIndex(x => x.SaleId);
            builder.HasIndex(x => x.ServiceId);
            builder.HasIndex(x => x.ProductId);
            builder.HasIndex(x => x.EmployeeId);
        }
    }
}
