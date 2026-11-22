using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class LocalityConfiguration : IEntityTypeConfiguration<Locality>
    {
        public void Configure(EntityTypeBuilder<Locality> builder)
        {
            builder.ToTable("Localities");

            builder.HasKey(x => x.Id);

            // Seed verisi kaynak SQL'deki locality ID'lerini explicit olarak
            // yazdigi icin IDENTITY uretimi kapatilir.
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.DistrictId)
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

            builder.HasOne(x => x.District)
                .WithMany(x => x.Localities)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.DistrictId)
                .HasDatabaseName("IX_Localities_DistrictId");

            builder.HasIndex(x => x.SearchName)
                .HasDatabaseName("IX_Localities_SearchName");

            builder.HasIndex(x => new { x.DistrictId, x.SearchName })
                .HasDatabaseName("IX_Localities_DistrictId_SearchName");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Localities_IsActive");
        }
    }
}
