using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Musteri x kanal x amac guncel izni. Tek kayit per kombinasyon;
    /// gecmis ConsentHistory'den izlenir.
    /// </summary>
    public class CustomerCommunicationPreferenceConfiguration : BaseConfiguration<CustomerCommunicationPreference>
    {
        public override void Configure(EntityTypeBuilder<CustomerCommunicationPreference> builder)
        {
            base.Configure(builder);

            builder.ToTable("CustomerCommunicationPreferences");

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

            builder.Property(x => x.LegalBasis)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.EvidenceReference)
                .IsRequired(false)
                .HasMaxLength(200);

            builder.Property(x => x.IsAllowed)
                .IsRequired();

            // Iliskiler acikca Restrict: convention cascade SQL Server'da
            // multiple cascade path hatasi uretir.
            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId, x.Channel, x.Purpose })
                .IsUnique()
                .HasDatabaseName("UX_CommunicationPreferences_Business_Customer_Channel_Purpose");
        }
    }
}
