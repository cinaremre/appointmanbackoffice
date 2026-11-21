using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Push cihaz token kayitlari. Ayni kullanici + token cifti tektir;
    /// pasif token'lar kayit olarak kalir (soft delete yerine IsActive bayragi).
    /// </summary>
    public class DeviceTokenConfiguration : BaseConfiguration<DeviceToken>
    {
        public override void Configure(EntityTypeBuilder<DeviceToken> builder)
        {
            base.Configure(builder);

            builder.ToTable("DeviceTokens");

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.Platform)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.LastSeenAtUtc)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.Token })
                .IsUnique()
                .HasDatabaseName("UX_DeviceTokens_UserId_Token");

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_DeviceTokens_UserId");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_DeviceTokens_IsActive");
        }
    }
}
