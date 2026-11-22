using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Yonetici denetim kaydi. Append-only defter: update/delete API'si
    /// bilincli olarak sunulmaz. Sorgular isletme ve tarih uzerinden.
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

            // Denetim kayitlari fiziksel olarak silinmez: EF soft delete'i
            // (IsDeleted) devre disi birakmak yerine query filter yalnizca
            // aktif kayitlara uygulanmaz; kayitlar append-only tutulur.
            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_AdminAuditLogs_BusinessId");

            builder.HasIndex(x => x.ActorUserId)
                .HasDatabaseName("IX_AdminAuditLogs_ActorUserId");

            builder.HasIndex(x => x.CreatedAtUtc)
                .HasDatabaseName("IX_AdminAuditLogs_CreatedAtUtc");
        }
    }
}
