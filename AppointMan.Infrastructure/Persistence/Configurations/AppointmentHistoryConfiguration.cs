using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class AppointmentHistoryConfiguration : BaseConfiguration<AppointmentHistory>
    {
        public override void Configure(EntityTypeBuilder<AppointmentHistory> builder)
        {
            base.Configure(builder);

            builder.ToTable("AppointmentHistories");

            builder.Property(x => x.AppointmentId)
                .IsRequired();

            builder.Property(x => x.OldStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.NewStatus)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.ChangeReason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.ChangedAt)
                .IsRequired();

            builder.Property(x => x.ChangedBy)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.IpAddress)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(x => x.UserAgent)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.AdditionalData)
                .IsRequired(false)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false }),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, new JsonSerializerOptions()) ?? new Dictionary<string, object>()
                )
                .HasColumnName("AdditionalData");

            builder.HasOne(x => x.Appointment)
                .WithMany(x => x.Histories)
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.AppointmentId);
            builder.HasIndex(x => x.ChangedAt);
            builder.HasIndex(x => x.NewStatus);
            builder.HasIndex(x => new { x.AppointmentId, x.ChangedAt }).IsDescending(false, true);
        }
    }
}