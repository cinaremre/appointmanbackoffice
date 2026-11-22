using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Isletmenin plana baglanmasi. Isletme basina aktif abonelik benzersizdir;
    /// yeni atamada eskisi handler tarafindan Expired yapilir.
    /// </summary>
    public class BusinessSubscriptionConfiguration : BaseConfiguration<BusinessSubscription>
    {
        public override void Configure(EntityTypeBuilder<BusinessSubscription> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessSubscriptions");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.PlanId)
                .IsRequired();

            builder.Property(x => x.StartAt)
                .IsRequired();

            builder.Property(x => x.EndAt)
                .IsRequired(false);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.HasOne(x => x.Plan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.Status })
                .HasDatabaseName("IX_BusinessSubscriptions_BusinessId_Status");
        }
    }
}
