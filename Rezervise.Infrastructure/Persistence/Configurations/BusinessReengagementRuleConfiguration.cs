using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Isletmenin geri cagirma kurali. ServiceId null ise tum hizmetler
    /// icin gecerlidir.
    /// </summary>
    public class BusinessReengagementRuleConfiguration : BaseConfiguration<BusinessReengagementRule>
    {
        public override void Configure(EntityTypeBuilder<BusinessReengagementRule> builder)
        {
            base.Configure(builder);

            builder.ToTable("BusinessReengagementRules");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired(false);

            builder.Property(x => x.TemplateMessage)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.IntervalDays)
                .IsRequired();

            builder.Property(x => x.LeadDays)
                .IsRequired();

            builder.Property(x => x.MaxMessagesPerCycle)
                .IsRequired();

            builder.Property(x => x.CooldownDays)
                .IsRequired();

            builder.Property(x => x.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            // Iliskiler acikca Restrict: convention cascade SQL Server'da
            // multiple cascade path hatasi uretir.
            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.ServiceId })
                .HasDatabaseName("IX_BusinessReengagementRules_Business_Service");
        }
    }
}
