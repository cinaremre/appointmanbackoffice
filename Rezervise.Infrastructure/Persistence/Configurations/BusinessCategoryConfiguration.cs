using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class BusinessCategoryConfiguration : BaseConfiguration<BusinessCategory>
    {
        public override void Configure(EntityTypeBuilder<BusinessCategory> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessCategories");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IconUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.ImageUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CorrespondingBusinessType)
                .IsRequired(false)
                .HasConversion<int>();

            builder.Property(x => x.ParentCategoryId)
                .IsRequired(false);

            builder.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Businesses)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.Name).IsUnique();
            builder.HasIndex(x => x.DisplayOrder);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.ParentCategoryId);
            builder.HasIndex(x => x.CorrespondingBusinessType);
        }
    }
}