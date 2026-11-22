using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Veri aktarim satir sonucu. SourceRecordKey normalize telefon
    /// degeridir; deterministik mukerrer kontrolunde kullanilir.
    /// </summary>
    public class ImportRowResultConfiguration : BaseConfiguration<ImportRowResult>
    {
        public override void Configure(EntityTypeBuilder<ImportRowResult> builder)
        {
            base.Configure(builder);

            builder.ToTable("ImportRowResults");

            builder.Property(x => x.ImportJobId)
                .IsRequired();

            builder.Property(x => x.RowNumber)
                .IsRequired();

            builder.Property(x => x.SourceRecordKey)
                .IsRequired(false)
                .HasMaxLength(20);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.ErrorDetails)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.HasIndex(x => x.ImportJobId)
                .HasDatabaseName("IX_ImportRowResults_ImportJobId");

            builder.HasIndex(x => new { x.ImportJobId, x.Status })
                .HasDatabaseName("IX_ImportRowResults_ImportJobId_Status");
        }
    }
}
