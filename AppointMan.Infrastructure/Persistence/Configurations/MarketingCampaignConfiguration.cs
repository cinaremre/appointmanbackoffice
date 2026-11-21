using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Pazarlama kampanyasi. Harcama ve randevu atiflari bu kampanyaya baglanir.
    /// </summary>
    public class MarketingCampaignConfiguration : BaseConfiguration<MarketingCampaign>
    {
        public override void Configure(EntityTypeBuilder<MarketingCampaign> builder)
        {
            base.Configure(builder);

            builder.ToTable("MarketingCampaigns");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Channel)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.ExternalCampaignId)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.StartAt)
                .IsRequired(false);

            builder.Property(x => x.EndAt)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_MarketingCampaigns_BusinessId");
        }
    }
}
