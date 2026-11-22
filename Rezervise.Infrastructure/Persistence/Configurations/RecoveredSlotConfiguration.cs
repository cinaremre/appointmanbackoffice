using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Iptal edilen randevudan aciga cikan slot. Kurtarilan kapasite
    /// raporlari bu kayitlardan uretilir.
    /// </summary>
    public class RecoveredSlotConfiguration : BaseConfiguration<RecoveredSlot>
    {
        public override void Configure(EntityTypeBuilder<RecoveredSlot> builder)
        {
            base.Configure(builder);

            builder.ToTable("RecoveredSlots");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CancelledAppointmentId)
                .IsRequired();

            builder.Property(x => x.ReplacementAppointmentId)
                .IsRequired(false);

            builder.Property(x => x.ReleasedAt)
                .IsRequired();

            builder.Property(x => x.RebookedAt)
                .IsRequired(false);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_RecoveredSlots_BusinessId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_RecoveredSlots_Status");
        }
    }
}
