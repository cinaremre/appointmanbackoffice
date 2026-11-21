using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Donemsel ozellik kullanim sayaci. (BusinessId, FeatureId, UsagePeriod)
    /// benzersizdir; upsert FeatureAccessService tarafindan yapilir.
    /// </summary>
    public class FeatureUsageConfiguration : BaseConfiguration<FeatureUsage>
    {
        public override void Configure(EntityTypeBuilder<FeatureUsage> builder)
        {
            base.Configure(builder);

            builder.ToTable("FeatureUsages");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.FeatureId)
                .IsRequired();

            builder.Property(x => x.UsagePeriod)
                .IsRequired()
                .HasMaxLength(7);

            builder.Property(x => x.UsedQuantity)
                .IsRequired();

            // Iliskiler acikca Restrict: convention cascade SQL Server'da
            // multiple cascade path hatasi uretir.
            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Feature)
                .WithMany()
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.FeatureId, x.UsagePeriod })
                .IsUnique()
                .HasDatabaseName("UX_FeatureUsages_Business_Feature_Period");
        }
    }
}
