using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Prim odeme donemi. Donem kapatilirken odenen toplam prim tutari
    /// kaydedilir; donem baslangic/bitis UTC olarak tutulur.
    /// </summary>
    public class CommissionPayoutConfiguration : BaseConfiguration<CommissionPayout>
    {
        public override void Configure(EntityTypeBuilder<CommissionPayout> builder)
        {
            base.Configure(builder);

            builder.ToTable("CommissionPayouts");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.PeriodStart)
                .IsRequired();

            builder.Property(x => x.PeriodEnd)
                .IsRequired();

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.PaidAt)
                .IsRequired(false);

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_CommissionPayouts_BusinessId");

            builder.HasIndex(x => x.EmployeeId)
                .HasDatabaseName("IX_CommissionPayouts_EmployeeId");
        }
    }
}
