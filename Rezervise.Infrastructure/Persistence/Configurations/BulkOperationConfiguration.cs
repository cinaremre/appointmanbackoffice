using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Toplu islem kaydinin EF Core eslemesi (dokuman Modul 10).
    /// Append-only alanlar (payload, istekci) yalnizca insert'te yazilir;
    /// durum gecisleri entity metotlari uzerinden yurutulur.
    /// </summary>
    public class BulkOperationConfiguration : BaseConfiguration<BulkOperation>
    {
        public override void Configure(EntityTypeBuilder<BulkOperation> builder)
        {
            base.Configure(builder);

            builder.ToTable("BulkOperations");

            builder.Property(x => x.BusinessId)
                .IsRequired(false);

            builder.Property(x => x.OperationType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.RequestedPayload)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.DryRun)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.RequestedBy)
                .IsRequired();

            builder.Property(x => x.TotalRecords)
                .IsRequired();

            builder.Property(x => x.SuccessCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.FailedCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ErrorReport)
                .IsRequired(false)
                .HasMaxLength(4000);

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.Property(x => x.StartedAtUtc)
                .IsRequired(false);

            builder.Property(x => x.CompletedAtUtc)
                .IsRequired(false);

            // Islemci taramalari: bekleyen isler ve isletme bazli filtre.
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.OperationType);
        }
    }
}
