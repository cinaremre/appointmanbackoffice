using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Prim (komisyon) kurali. Uygulama penceresi EffectiveFrom/EffectiveTo
    /// ile sinirlidir; kapsam alanlari bos ise kural geneldir.
    /// </summary>
    public class CommissionRuleConfiguration : BaseConfiguration<CommissionRule>
    {
        public override void Configure(EntityTypeBuilder<CommissionRule> builder)
        {
            base.Configure(builder);

            builder.ToTable("CommissionRules");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.EmployeeId)
                .IsRequired(false);

            builder.Property(x => x.ServiceId)
                .IsRequired(false);

            builder.Property(x => x.ProductId)
                .IsRequired(false);

            builder.Property(x => x.CalculationType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Rate)
                .IsRequired()
                .HasPrecision(5, 4);

            builder.Property(x => x.FixedAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.EffectiveFrom)
                .IsRequired();

            builder.Property(x => x.EffectiveTo)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_CommissionRules_BusinessId");

            builder.HasIndex(x => new { x.BusinessId, x.EmployeeId, x.ServiceId })
                .HasDatabaseName("IX_CommissionRules_Business_Employee_Service");
        }
    }
}
