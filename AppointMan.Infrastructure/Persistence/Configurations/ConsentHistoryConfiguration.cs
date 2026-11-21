using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Iletisim izni degisikliklerinin degismez kaydi. Append-only;
    /// denetim ve hukuki ispat icin tutulur.
    /// </summary>
    public class ConsentHistoryConfiguration : BaseConfiguration<ConsentHistory>
    {
        public override void Configure(EntityTypeBuilder<ConsentHistory> builder)
        {
            base.Configure(builder);

            builder.ToTable("ConsentHistories");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.Channel)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Purpose)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Action)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Source)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.EvidenceReference)
                .IsRequired(false)
                .HasMaxLength(200);

            builder.Property(x => x.OccurredAt)
                .IsRequired();

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId })
                .HasDatabaseName("IX_ConsentHistories_Business_Customer");
        }
    }
}
