using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class CashSessionConfiguration : BaseConfiguration<CashSession>
    {
        public override void Configure(EntityTypeBuilder<CashSession> builder)
        {
            base.Configure(builder);

            builder.ToTable("CashSessions");

            builder.Property(x => x.OpeningCash)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.ExpectedCash)
                .IsRequired(false)
                .HasPrecision(18, 2);

            builder.Property(x => x.CountedCash)
                .IsRequired(false)
                .HasPrecision(18, 2);

            builder.Property(x => x.Difference)
                .IsRequired(false)
                .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumCashSessionStatus.Open);

            builder.Property(x => x.OpenedBy)
                .IsRequired();

            builder.Property(x => x.ClosedBy)
                .IsRequired(false);

            builder.Property(x => x.OpenedAt)
                .IsRequired();

            builder.Property(x => x.ClosedAt)
                .IsRequired(false);

            builder.Property(x => x.CashRegisterId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.HasOne(x => x.CashRegister)
                .WithMany()
                .HasForeignKey(x => x.CashRegisterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CashRegisterId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.Status);

            // Aynı kasada aynı anda tek açık oturum olabilir.
            // Yalnızca açık (0) ve silinmemiş satırlar filtreye girer;
            // es zamanlı iki açış denemesinde ikincisi unique index
            // ihlali alır ve handler 409 Conflict döner.
            builder.HasIndex(x => x.CashRegisterId)
                .IsUnique()
                .HasFilter("[Status] = 0 AND [IsDeleted] = 0")
                .HasDatabaseName("UX_CashSessions_CashRegisterId_Open");
        }
    }
}
