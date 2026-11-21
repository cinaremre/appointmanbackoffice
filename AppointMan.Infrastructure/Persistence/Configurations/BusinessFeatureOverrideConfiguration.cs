using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Isletme bazinda ozellik override'i. Aktif override abonelik
    /// planindaki degerleri ezer; sureli verilebilir.
    /// </summary>
    public class BusinessFeatureOverrideConfiguration : BaseConfiguration<BusinessFeatureOverride>
    {
        public override void Configure(EntityTypeBuilder<BusinessFeatureOverride> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessFeatureOverrides");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.FeatureId)
                .IsRequired();

            builder.Property(x => x.IsEnabled)
                .IsRequired();

            builder.Property(x => x.QuotaOverride)
                .IsRequired(false);

            builder.Property(x => x.ValidFrom)
                .IsRequired();

            builder.Property(x => x.ValidUntil)
                .IsRequired(false);

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.GrantedBy)
                .IsRequired();

            builder.HasOne(x => x.Feature)
                .WithMany()
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.FeatureId })
                .HasDatabaseName("IX_BusinessFeatureOverrides_BusinessId_FeatureId");
        }
    }
}
