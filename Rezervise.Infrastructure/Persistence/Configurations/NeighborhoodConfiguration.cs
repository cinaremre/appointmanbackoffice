using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class NeighborhoodConfiguration : IEntityTypeConfiguration<Neighborhood>
    {
        public void Configure(EntityTypeBuilder<Neighborhood> builder)
        {
            builder.ToTable("Neighborhoods");

            builder.HasKey(x => x.Id);

            // Mahalle seed'i kaynak verideki ID'leri explicit yazacagi icin
            // IDENTITY uretimi kapatilir.
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.LocalityId)
                .IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.SearchName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(x => x.Locality)
                .WithMany(x => x.Neighborhoods)
                .HasForeignKey(x => x.LocalityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.LocalityId)
                .HasDatabaseName("IX_Neighborhoods_LocalityId");

            builder.HasIndex(x => x.SearchName)
                .HasDatabaseName("IX_Neighborhoods_SearchName");

            builder.HasIndex(x => new { x.LocalityId, x.SearchName })
                .HasDatabaseName("IX_Neighborhoods_LocalityId_SearchName");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Neighborhoods_IsActive");
        }
    }
}
