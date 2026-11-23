using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Tek bir geri çağırma denemesi. Eligible -> Notified -> Converted
    /// biçiminde ilerler; izin engelleri Suppressed, cooldown Cooldown olur.
    /// </summary>
    public class ReengagementAttemptConfiguration : BaseConfiguration<ReengagementAttempt>
    {
        public override void Configure(EntityTypeBuilder<ReengagementAttempt> builder)
        {
            base.Configure(builder);

            builder.ToTable("ReengagementAttempts");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.RuleId)
                .IsRequired();

            builder.Property(x => x.NotificationId)
                .IsRequired(false);

            builder.Property(x => x.TriggeredAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.ConvertedAppointmentId)
                .IsRequired(false);

            builder.HasOne(x => x.Rule)
                .WithMany()
                .HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId, x.ServiceId })
                .HasDatabaseName("IX_ReengagementAttempts_Business_Customer_Service");

            builder.HasIndex(x => x.TriggeredAt)
                .HasDatabaseName("IX_ReengagementAttempts_TriggeredAt");
        }
    }
}
