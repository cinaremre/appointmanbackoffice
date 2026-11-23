using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Yönetici denetim kaydı. Append-only defter: update/delete API'si
    /// bilinçli olarak sunulmaz. Sorgular işletme ve tarih üzerinden.
    /// </summary>
    public class AdminAuditLogConfiguration : BaseConfiguration<AdminAuditLog>
    {
        public override void Configure(EntityTypeBuilder<AdminAuditLog> builder)
        {
            base.Configure(builder);

            builder.ToTable("AdminAuditLogs");

            builder.Property(x => x.ActorUserId)
                .IsRequired();

            builder.Property(x => x.ActorRole)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.BusinessId)
                .IsRequired(false);

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.EntityId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.OldValues)
                .IsRequired(false)
                .HasMaxLength(2000);

            builder.Property(x => x.NewValues)
                .IsRequired(false)
                .HasMaxLength(2000);

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.Property(x => x.CorrelationId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.IpAddress)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // Denetim kayıtları fiziksel olarak silinmez: EF soft delete'i
            // (IsDeleted) devre dışı bırakmak yerine query filter yalnızca
            // aktif kayıtlara uygulanmaz; kayıtlar append-only tutulur.
            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_AdminAuditLogs_BusinessId");

            builder.HasIndex(x => x.ActorUserId)
                .HasDatabaseName("IX_AdminAuditLogs_ActorUserId");

            builder.HasIndex(x => x.CreatedAtUtc)
                .HasDatabaseName("IX_AdminAuditLogs_CreatedAtUtc");
        }
    }
}
