using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
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

            // Müşteri (role 0) e-postası opsiyonel; diğer rollerde FluentValidation zorunlu tutar.
            builder.Property(x => x.Email)
                .IsRequired(false)
                .HasMaxLength(255)
                .HasColumnName("Email");

            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("PhoneNumber")
                .HasConversion(
                    v => v.Value,
                    v => new PhoneNumber(v)
                );

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.AccountStatus)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumAccountStatus.Active);

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

            // Güvenlik: e-posta doğrulama token'i yalnızca SHA-256 hash'i olarak saklanır.
            builder.Property(x => x.EmailVerificationTokenHash)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(x => x.EmailVerificationTokenExpiresAt)
                .IsRequired(false);

            builder.Property(x => x.EmailVerificationRequestedAt)
                .IsRequired(false);

            builder.HasIndex(x => x.EmailVerificationTokenHash)
                .HasDatabaseName("IX_Users_EmailVerificationTokenHash")
                .HasFilter("[EmailVerificationTokenHash] IS NOT NULL");

            // Güvenlik: telefon doğrulama kodu da yalnızca SHA-256 hash'i olarak saklanır.
            builder.Property(x => x.PhoneVerificationCodeHash)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(x => x.PhoneVerificationCodeExpiresAt)
                .IsRequired(false);

            builder.Property(x => x.PhoneVerificationRequestedAt)
                .IsRequired(false);

            builder.Property(x => x.PhoneVerificationAttemptCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(x => x.PhoneVerificationCodeHash)
                .HasDatabaseName("IX_Users_PhoneVerificationCodeHash")
                .HasFilter("[PhoneVerificationCodeHash] IS NOT NULL");

            builder.Property(x => x.LastLoginDate)
                .IsRequired(false);

            // Güvenlik: refresh token yalnızca SHA-256 hash'i olarak saklanır.
            builder.Property(x => x.RefreshTokenHash)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(x => x.PreviousRefreshTokenHash)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(x => x.RefreshTokenExpiresAtUtc)
                .IsRequired(false);

            builder.Property(x => x.RefreshTokenCreatedAtUtc)
                .IsRequired(false);

            builder.Property(x => x.RefreshTokenRevokedAtUtc)
                .IsRequired(false);

            // Güvenlik: brute force koruması.
            builder.Property(x => x.AccessFailedCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.LockoutEndUtc)
                .IsRequired(false);

            builder.Property(x => x.LastFailedLoginUtc)
                .IsRequired(false);

            // Konum: kullanıcının kayıtlı öldüğü il / ilçe.
            builder.Property(x => x.CityId)
                .IsRequired(false);

            builder.Property(x => x.DistrictId)
                .IsRequired(false);

            // Dil tercihi: "tr" / "en". NOT NULL + varsayılan "tr" öldüğü için
            // mevcut satırlar için backfill gerekmez.
            builder.Property(x => x.LanguageCode)
                .IsRequired()
                .HasMaxLength(2)
                .HasDefaultValue(User.DefaultLanguageCode);

            builder.HasOne<City>()
                .WithMany()
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<District>()
                .WithMany()
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

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

            builder.HasMany(x => x.Employees)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.BlacklistEntries)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index'ler
            // Filtreli unique index: e-postası olmayan (NULL) birden fazla müşteriye izin verir.
            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("UX_Users_Email")
                .HasFilter("[Email] IS NOT NULL");
            builder.HasIndex(x => x.PhoneNumber).IsUnique();
            builder.HasIndex(x => x.AccountStatus);
            builder.HasIndex(x => x.CityId);
            builder.HasIndex(x => x.DistrictId);
            builder.HasIndex(x => x.LanguageCode);
        }
    }
}