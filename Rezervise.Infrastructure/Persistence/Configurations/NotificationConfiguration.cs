using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : BaseConfiguration<Notification>
    {
        public override void Configure(EntityTypeBuilder<Notification> builder)
        {
            base.Configure(builder);

            builder.ToTable("Notifications");

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.ReadAt)
                .IsRequired(false);

            builder.Property(x => x.SentAt)
                .IsRequired();

            builder.Property(x => x.ActionUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            // Metadata için Value Converter
            builder.Property(x => x.Metadata)
                .IsRequired(false)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, new JsonSerializerOptions()) ?? new Dictionary<string, string>()
                )
                .HasColumnName("Metadata");

            builder.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.IsRead);
            builder.HasIndex(x => x.SentAt);
        }
    }
}