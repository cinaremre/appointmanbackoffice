using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Kampanya harcaması. Manuel giriş veya API senkronu ile yazılır;
    /// ilk sürümde aynı gün+tutar tekrarı bilinçli olarak serbesttir.
    /// </summary>
    public class CampaignSpendConfiguration : BaseConfiguration<CampaignSpend>
    {
        public override void Configure(EntityTypeBuilder<CampaignSpend> builder)
        {
            base.Configure(builder);

            builder.ToTable("CampaignSpends");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CampaignId)
                .IsRequired();

            builder.Property(x => x.SpendDate)
                .IsRequired();

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.DataSource)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasIndex(x => x.CampaignId)
                .HasDatabaseName("IX_CampaignSpends_CampaignId");

            builder.HasIndex(x => new { x.BusinessId, x.SpendDate })
                .HasDatabaseName("IX_CampaignSpends_BusinessId_SpendDate");
        }
    }
}
