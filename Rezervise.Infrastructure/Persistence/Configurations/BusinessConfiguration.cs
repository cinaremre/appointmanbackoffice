using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class BusinessConfiguration : BaseConfiguration<Business>
    {
        public override void Configure(EntityTypeBuilder<Business> builder)
        {
            base.Configure(builder);

            builder.ToTable("Businesses");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.BusinessType)
                .IsRequired()
                .HasConversion<int>();

            builder.OwnsOne(x => x.Address, address =>
            {
                address.Property(a => a.CityId)
                    .HasColumnName("CityId")
                    .IsRequired();

                address.Property(a => a.DistrictId)
                    .HasColumnName("DistrictId")
                    .IsRequired(false);

                address.Property(a => a.LocalityId)
                    .HasColumnName("LocalityId")
                    .IsRequired(false);

                address.Property(a => a.NeighborhoodId)
                    .HasColumnName("NeighborhoodId")
                    .IsRequired(false);

                address.Property(a => a.Street)
                    .HasColumnName("Street")
                    .IsRequired()
                    .HasMaxLength(100);

                address.Property(a => a.BuildingNumber)
                    .HasColumnName("BuildingNumber")
                    .IsRequired(false)
                    .HasMaxLength(20);

                address.Property(a => a.ApartmentNumber)
                    .HasColumnName("ApartmentNumber")
                    .IsRequired(false)
                    .HasMaxLength(20);

                address.Property(a => a.PostalCode)
                    .HasColumnName("PostalCode")
                    .IsRequired(false)
                    .HasMaxLength(20);

                address.OwnsOne(a => a.Coordinates, coordinates =>
                {
                    coordinates.Property(c => c.Latitude)
                        .HasColumnName("Latitude")
                        .IsRequired(false)
                        .HasPrecision(10, 6);

                    coordinates.Property(c => c.Longitude)
                        .HasColumnName("Longitude")
                        .IsRequired(false)
                        .HasPrecision(10, 6);
                });

                address.HasOne<City>()
                    .WithMany()
                    .HasForeignKey(a => a.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                address.HasOne<District>()
                    .WithMany()
                    .HasForeignKey(a => a.DistrictId)
                    .OnDelete(DeleteBehavior.Restrict);

                address.HasOne<Locality>()
                    .WithMany()
                    .HasForeignKey(a => a.LocalityId)
                    .OnDelete(DeleteBehavior.Restrict);

                address.HasOne<Neighborhood>()
                    .WithMany()
                    .HasForeignKey(a => a.NeighborhoodId)
                    .OnDelete(DeleteBehavior.Restrict);

                address.HasIndex(a => a.CityId)
                    .HasDatabaseName("IX_Businesses_CityId");

                address.HasIndex(a => a.DistrictId)
                    .HasDatabaseName("IX_Businesses_DistrictId");

                address.HasIndex(a => a.LocalityId)
                    .HasDatabaseName("IX_Businesses_LocalityId");

                address.HasIndex(a => a.NeighborhoodId)
                    .HasDatabaseName("IX_Businesses_NeighborhoodId");

                address.HasIndex(a => new { a.CityId, a.DistrictId })
                    .HasDatabaseName("IX_Businesses_CityId_DistrictId");
            });

            builder.OwnsOne(x => x.PhoneNumber, phone =>
            {
                phone.Property(p => p.Value)
                    .HasColumnName("PhoneNumber")
                    .IsRequired()
                    .HasMaxLength(20);
            });

            builder.OwnsOne(x => x.Email, email =>
            {
                email.Property(e => e.Value)
                    .HasColumnName("Email")
                    .IsRequired()
                    .HasMaxLength(255);
            });

            builder.Property(x => x.WebsiteUrl)
                .IsRequired(false)
                .HasMaxLength(200);

            builder.Property(x => x.LogoUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.Rating)
                .IsRequired(false)
                .HasPrecision(3, 1);

            builder.Property(x => x.TotalReviews)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.OwnerId)
                .IsRequired();

            builder.Property(x => x.CategoryId)
                .IsRequired(false);

            builder.HasOne(x => x.Owner)
                .WithMany(x => x.OwnedBusinesses)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Businesses)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Services)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.WorkingHours)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Appointments)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Reviews)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Favorites)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Employees)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.BlacklistEntries)
                .WithOne(x => x.Business)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.Name);
            builder.HasIndex(x => x.OwnerId);
            builder.HasIndex(x => x.BusinessType);
            builder.HasIndex(x => new { x.CategoryId, x.IsActive, x.IsVerified });
        }
    }
}