using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class ServiceCategoryTranslationConfiguration : BaseConfiguration<ServiceCategoryTranslation>
    {
        public override void Configure(EntityTypeBuilder<ServiceCategoryTranslation> builder)
        {
            base.Configure(builder);

            builder.ToTable("ServiceCategoryTranslations");

            builder.Property(x => x.ServiceCategoryId)
                .IsRequired();

            builder.Property(x => x.LanguageCode)
                .IsRequired()
                .HasMaxLength(5);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.HasOne(x => x.ServiceCategory)
                .WithMany(x => x.Translations)
                .HasForeignKey(x => x.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.LanguageCode);
            builder.HasIndex(x => new { x.ServiceCategoryId, x.LanguageCode }).IsUnique();
            builder.HasIndex(x => new { x.LanguageCode, x.Name });
        }
    }
}