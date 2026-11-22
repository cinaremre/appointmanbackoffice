using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Prim tahakkuku. Tahsilat tabanli hesaplanir; Accrued -> Paid/Reversed
    /// yonunde tek yonlu durum degisimi yapilir.
    /// </summary>
    public class CommissionAccrualConfiguration : BaseConfiguration<CommissionAccrual>
    {
        public override void Configure(EntityTypeBuilder<CommissionAccrual> builder)
        {
            base.Configure(builder);

            builder.ToTable("CommissionAccruals");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.SaleLineId)
                .IsRequired(false);

            builder.Property(x => x.RuleId)
                .IsRequired();

            builder.Property(x => x.BaseAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.CommissionAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.CalculatedAt)
                .IsRequired();

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_CommissionAccruals_BusinessId");

            builder.HasIndex(x => x.EmployeeId)
                .HasDatabaseName("IX_CommissionAccruals_EmployeeId");

            builder.HasIndex(x => x.SaleLineId)
                .HasDatabaseName("IX_CommissionAccruals_SaleLineId");
        }
    }
}
