using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Seans paketi tanimi. Items cocuk koleksiyonu aggregate ici iliskiyle
    /// tutulur; kalan hak hareketlerden hesaplanir.
    /// </summary>
    public class ServicePackageConfiguration : BaseConfiguration<ServicePackage>
    {
        public override void Configure(EntityTypeBuilder<ServicePackage> builder)
        {
            base.Configure(builder);

            builder.ToTable("ServicePackages");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Price)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(x => x.ValidityDays)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Items cocuk koleksiyonu: EF backing field uzerinden erisir.
            builder.Navigation(x => x.Items)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.ServicePackage)
                .HasForeignKey(x => x.ServicePackageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_ServicePackages_BusinessId");
        }
    }
}
