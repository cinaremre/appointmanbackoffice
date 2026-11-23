using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class CashMovementConfiguration : BaseConfiguration<CashMovement>
    {
        public override void Configure(EntityTypeBuilder<CashMovement> builder)
        {
            base.Configure(builder);

            builder.ToTable("CashMovements");

            builder.Property(x => x.MovementType)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumCashMovementType.CashIn);

            builder.Property(x => x.Amount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.ReferenceId)
                .IsRequired(false);

            builder.Property(x => x.CreatedBy)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.CashSessionId)
                .IsRequired();

            // Oturum kapanış hesabı (giriş/çıkış toplamları) için index.
            builder.HasIndex(x => x.CashSessionId)
                .HasDatabaseName("IX_CashMovements_CashSessionId");
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.MovementType);
        }
    }
}
