using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Musteri x hizmet ziyaret ozeti. Geri cagirma aday hesabinin kaynagi;
    /// medyan ziyaret araligi VisitCount >= 2 iken anlamlidir.
    /// </summary>
    public class CustomerServiceHistoryConfiguration : BaseConfiguration<CustomerServiceHistory>
    {
        public override void Configure(EntityTypeBuilder<CustomerServiceHistory> builder)
        {
            base.Configure(builder);

            builder.ToTable("CustomerServiceHistories");

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.LastCompletedAt)
                .IsRequired();

            builder.Property(x => x.VisitCount)
                .IsRequired();

            builder.Property(x => x.MedianIntervalDays)
                .IsRequired(false);

            // Iliskiler acikca Restrict tanimlanir: EF convention'in cascade
            // secimi SQL Server'da "multiple cascade paths" hatasi uretir.
            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Service)
                .WithMany()
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BusinessId, x.CustomerId, x.ServiceId })
                .IsUnique()
                .HasDatabaseName("UX_CustomerServiceHistories_Business_Customer_Service");

            builder.HasIndex(x => x.LastCompletedAt)
                .HasDatabaseName("IX_CustomerServiceHistories_LastCompletedAt");
        }
    }
}
