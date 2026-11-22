using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class ServiceCategoryConfiguration : BaseConfiguration<ServiceCategory>
    {
        public override void Configure(EntityTypeBuilder<ServiceCategory> builder)
        {
            base.Configure(builder);

            builder.ToTable("ServiceCategories");

            builder.Property(x => x.IconUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.ColorHex)
                .IsRequired(false)
                .HasMaxLength(10);

            builder.Property(x => x.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.ParentCategoryId)
                .IsRequired(false);

            builder.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Translations)
                .WithOne(x => x.ServiceCategory)
                .HasForeignKey(x => x.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Services)
                .WithOne(x => x.ServiceCategory)
                .HasForeignKey(x => x.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ParentCategoryId);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.DisplayOrder);
        }
    }
}