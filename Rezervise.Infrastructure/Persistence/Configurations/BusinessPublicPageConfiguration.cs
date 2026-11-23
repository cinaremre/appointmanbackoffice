using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// İşletmenin kamuya açık rezervasyon sayfası.
    /// Slug tüm sistemde benzersizdir; anonim erişim slug üzerinden yapılır.
    /// </summary>
    public class BusinessPublicPageConfiguration : BaseConfiguration<BusinessPublicPage>
    {
        public override void Configure(EntityTypeBuilder<BusinessPublicPage> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessPublicPages");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(60);

            builder.Property(x => x.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.ThemeSettings)
                .IsRequired(false)
                .HasMaxLength(2000);

            // Slug sistem genelinde benzersiz: iki işletme aynı rezervasyon
            // adresini kullanamaz. Yayın durumu filtreye girmez; yayından
            // kalkan sayfa slug'i elinde tutmaya devam eder.
            builder.HasIndex(x => x.Slug)
                .IsUnique()
                .HasDatabaseName("UX_BusinessPublicPages_Slug");

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_BusinessPublicPages_BusinessId");
        }
    }
}
