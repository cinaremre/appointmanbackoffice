using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Plana dahil edilen ozellik ve donemsel kotasi.
    /// Ayni plan + ozellik cifti bir kez tanimlanabilir.
    /// </summary>
    public class PlanFeatureConfiguration : BaseConfiguration<PlanFeature>
    {
        public override void Configure(EntityTypeBuilder<PlanFeature> builder)
        {
            base.Configure(builder);

            builder.ToTable("PlanFeatures");

            builder.Property(x => x.PlanId)
                .IsRequired();

            builder.Property(x => x.FeatureId)
                .IsRequired();

            builder.Property(x => x.IsEnabled)
                .IsRequired();

            builder.Property(x => x.IncludedQuota)
                .IsRequired(false);

            builder.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Feature)
                .WithMany()
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.PlanId, x.FeatureId })
                .IsUnique()
                .HasDatabaseName("UX_PlanFeatures_PlanId_FeatureId");
        }
    }
}
