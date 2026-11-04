using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : BaseConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.ToTable("Users");

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(50);

            // Email - string olarak
            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("Email");

            // ✅ PhoneNumber - string olarak (Value Object'i string'e çevir)
            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("PhoneNumber")
                .HasConversion(
                    v => v.Value,           // PhoneNumber -> string
                    v => new PhoneNumber(v) // string -> PhoneNumber
                );

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.DateOfBirth)
                .IsRequired(false);

            builder.Property(x => x.ProfilePictureUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IsEmailVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.IsPhoneVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.LastLoginDate)
                .IsRequired(false);

            builder.Property(x => x.RefreshToken)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.RefreshTokenExpiryTime)
                .IsRequired(false);

            // Relationships
            builder.HasMany(x => x.Appointments)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Reviews)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.OwnedBusinesses)
                .WithOne(x => x.Owner)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Favorites)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Notifications)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index'ler
            builder.HasIndex(x => x.Email).IsUnique();
            builder.HasIndex(x => x.PhoneNumber).IsUnique();
        }
    }
}