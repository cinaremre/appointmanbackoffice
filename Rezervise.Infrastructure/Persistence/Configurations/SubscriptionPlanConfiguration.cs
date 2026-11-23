using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Abonelik planı. Plan özellikleri PlanFeature kayıtlarıyla belirlenir.
    /// </summary>
    public class SubscriptionPlanConfiguration : BaseConfiguration<SubscriptionPlan>
    {
        public override void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            base.Configure(builder);

            builder.ToTable("SubscriptionPlans");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.BillingPeriod)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
        }
    }
}
