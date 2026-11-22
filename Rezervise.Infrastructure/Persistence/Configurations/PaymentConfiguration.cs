using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : BaseConfiguration<Payment>
    {
        public override void Configure(EntityTypeBuilder<Payment> builder)
        {
            base.Configure(builder);

            builder.ToTable("Payments");

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("TRY");

            builder.Property(x => x.PaymentMethod)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumPaymentMethod.Cash);

            builder.Property(x => x.PaidAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumSaleStatus.Completed);

            builder.Property(x => x.ExternalTransactionId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.IsRefund)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.RefersToPaymentId)
                .IsRequired(false);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.SaleId)
                .IsRequired();

            builder.Property(x => x.CashSessionId)
                .IsRequired(false);

            builder.HasOne<Sale>()
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bir odemenin kayitlari ters kayit baglantisiyla takip edilir.
            builder.HasIndex(x => x.SaleId)
                .HasDatabaseName("IX_Payments_SaleId");
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.CashSessionId);
            builder.HasIndex(x => x.PaymentMethod);
            builder.HasIndex(x => x.RefersToPaymentId);
        }
    }
}
