using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class BusinessServiceCategoryConfiguration : BaseConfiguration<BusinessServiceCategory>
    {
        public override void Configure(EntityTypeBuilder<BusinessServiceCategory> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessServiceCategories");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.ServiceCategoryId)
                .IsRequired();

            builder.HasOne(x => x.Business)
                .WithMany(x => x.ServiceCategories)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ServiceCategory)
                .WithMany()
                .HasForeignKey(x => x.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.ServiceCategoryId }).IsUnique();
            builder.HasIndex(x => x.ServiceCategoryId);
        }
    }
}