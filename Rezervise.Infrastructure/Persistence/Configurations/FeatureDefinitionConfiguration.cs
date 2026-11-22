using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Platform ozellik tanimi. Kod benzersizdir; plan ve override kayitlari
    /// bu tanima baglanir.
    /// </summary>
    public class FeatureDefinitionConfiguration : BaseConfiguration<FeatureDefinition>
    {
        public override void Configure(EntityTypeBuilder<FeatureDefinition> builder)
        {
            base.Configure(builder);

            builder.ToTable("FeatureDefinitions");

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("UX_FeatureDefinitions_Code");
        }
    }
}
