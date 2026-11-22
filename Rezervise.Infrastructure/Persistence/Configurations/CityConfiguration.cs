using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("Cities");

            builder.HasKey(x => x.Id);

            // Seed verisi plaka kodlarini (1-81) explicit Id olarak yazdigi icin
            // IDENTITY uretimi kapatilir.
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.SearchName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CountryCode)
                .IsRequired()
                .HasMaxLength(2)
                .IsFixedLength();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.HasMany(x => x.Districts)
                .WithOne(x => x.City)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SearchName)
                .HasDatabaseName("IX_Cities_SearchName");

            builder.HasIndex(x => x.CountryCode)
                .HasDatabaseName("IX_Cities_CountryCode");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Cities_IsActive");
        }
    }
}