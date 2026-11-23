using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class BlacklistEntryConfiguration : BaseConfiguration<BlacklistEntry>
    {
        public override void Configure(EntityTypeBuilder<BlacklistEntry> builder)
        {
            base.Configure(builder);

            builder.ToTable("BlacklistEntries");

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.AppointmentCount)
                .IsRequired();

            builder.Property(x => x.BlacklistedAt)
                .IsRequired();

            builder.Property(x => x.RemovedAt)
                .IsRequired(false);

            builder.Property(x => x.RemovedBy)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.RemoveReason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.HasOne(x => x.User)
                .WithMany(x => x.BlacklistEntries)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Business)
                .WithMany(x => x.BlacklistEntries)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BlacklistedAt);

            // Aynı kullanıcı aynı işletmede iki kez kara listede olamaz.
            // Es zamanlı iki ekleme denemesinde ikincisi DB tarafından reddedilir.
            builder.HasIndex(x => new { x.BusinessId, x.UserId })
                .IsUnique()
                .HasDatabaseName("UX_BlacklistEntries_BusinessId_UserId");
        }
    }
}