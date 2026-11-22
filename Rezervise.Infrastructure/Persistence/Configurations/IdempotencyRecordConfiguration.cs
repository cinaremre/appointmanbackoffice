using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Idempotency kayitlarinin sema eslemesi.
    /// Anahtar uzerinde tekillik vardir; ayni anahtarla ikinci kayit DB tarafindan reddedilir.
    /// </summary>
    public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.ToTable("IdempotencyRecords");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.AppointmentId)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.Key)
                .IsUnique()
                .HasDatabaseName("UX_IdempotencyRecords_Key");

            builder.HasIndex(x => x.AppointmentId)
                .HasDatabaseName("IX_IdempotencyRecords_AppointmentId");
        }
    }
}
