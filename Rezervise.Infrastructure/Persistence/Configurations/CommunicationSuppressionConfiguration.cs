using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Kanal bazlı tam gönderim engeli. Amaç fark etmeksizin o kanal
    /// kapatılır; yeni izin verildiğinde silinir.
    /// </summary>
    public class CommunicationSuppressionConfiguration : BaseConfiguration<CommunicationSuppression>
    {
        public override void Configure(EntityTypeBuilder<CommunicationSuppression> builder)
        {
            base.Configure(builder);

            builder.ToTable("CommunicationSuppressions");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.Channel)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(200);

            // İlişkiler açıkça Restrict: convention cascade SQL Server'da
            // multiple cascade path hatası üretir.
            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId, x.Channel })
                .IsUnique()
                .HasDatabaseName("UX_CommunicationSuppressions_Business_Customer_Channel");
        }
    }
}
