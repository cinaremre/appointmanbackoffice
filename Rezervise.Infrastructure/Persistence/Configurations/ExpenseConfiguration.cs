using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class ExpenseConfiguration : BaseConfiguration<Expense>
    {
        public override void Configure(EntityTypeBuilder<Expense> builder)
        {
            base.Configure(builder);

            builder.ToTable("Expenses");

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("TRY");

            builder.Property(x => x.ExpenseDate)
                .IsRequired();

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.PaymentMethod)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumPaymentMethod.Cash);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumSaleStatus.Completed);

            builder.Property(x => x.CategoryId)
                .IsRequired(false);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => x.ExpenseDate);

            // Günlük özet sorgusu için birleşik index.
            builder.HasIndex(x => new { x.BusinessId, x.ExpenseDate })
                .HasDatabaseName("IX_Expenses_BusinessId_ExpenseDate");
        }
    }
}
