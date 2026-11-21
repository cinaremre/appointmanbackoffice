using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Veri aktarim isi. CsvContent entity uzerinde tutulur ki Execute
    /// adimi satirlari yeniden isleyebilsin (ilk surum basitlestirmesi).
    /// </summary>
    public class ImportJobConfiguration : BaseConfiguration<ImportJob>
    {
        public override void Configure(EntityTypeBuilder<ImportJob> builder)
        {
            base.Configure(builder);

            builder.ToTable("ImportJobs");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.SourceSystem)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(260);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.ColumnMapping)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.CsvContent)
                .IsRequired();

            builder.Property(x => x.TotalRows)
                .IsRequired();

            builder.Property(x => x.ImportedRows)
                .IsRequired();

            builder.Property(x => x.FailedRows)
                .IsRequired();

            builder.Property(x => x.StartedAt)
                .IsRequired(false);

            builder.Property(x => x.CompletedAt)
                .IsRequired(false);

            builder.Property(x => x.CreatedBy)
                .IsRequired();

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_ImportJobs_BusinessId");
        }
    }
}
