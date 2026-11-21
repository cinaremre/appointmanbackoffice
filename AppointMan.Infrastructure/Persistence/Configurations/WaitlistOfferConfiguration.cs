using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Bekleme listesi slot teklifi. Sure dolan teklifler kabul edilemez;
    /// superseded teklifler ilk basarili kabuldan sonra olusur.
    /// </summary>
    public class WaitlistOfferConfiguration : BaseConfiguration<WaitlistOffer>
    {
        public override void Configure(EntityTypeBuilder<WaitlistOffer> builder)
        {
            base.Configure(builder);

            builder.ToTable("WaitlistOffers");

            builder.Property(x => x.WaitlistEntryId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.SlotStart)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.AcceptedAt)
                .IsRequired(false);

            builder.HasOne(x => x.WaitlistEntry)
                .WithMany()
                .HasForeignKey(x => x.WaitlistEntryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.WaitlistEntryId)
                .HasDatabaseName("IX_WaitlistOffers_WaitlistEntryId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_WaitlistOffers_Status");
        }
    }
}
