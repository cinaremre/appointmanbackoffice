using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class DistrictConfiguration : IEntityTypeConfiguration<District>
    {
        public void Configure(EntityTypeBuilder<District> builder)
        {
            builder.ToTable("Districts");

            builder.HasKey(x => x.Id);

            // Seed verisi kaynak SQL'deki ilce ID'lerini explicit olarak yazdigi
            // icin IDENTITY uretimi kapatilir.
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.CityId)
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

            builder.HasOne(x => x.City)
                .WithMany(x => x.Districts)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CityId)
                .HasDatabaseName("IX_Districts_CityId");

            builder.HasIndex(x => x.SearchName)
                .HasDatabaseName("IX_Districts_SearchName");

            builder.HasIndex(x => new { x.CityId, x.SearchName })
                .HasDatabaseName("IX_Districts_CityId_SearchName");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Districts_IsActive");
        }
    }
}