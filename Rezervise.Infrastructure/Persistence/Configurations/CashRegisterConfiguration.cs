using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class CashRegisterConfiguration : BaseConfiguration<CashRegister>
    {
        public override void Configure(EntityTypeBuilder<CashRegister> builder)
        {
            base.Configure(builder);

            builder.ToTable("CashRegisters");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.HasOne(x => x.Business)
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.IsActive);
        }
    }
}
