using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class EmployeeTimeOffConfiguration : BaseConfiguration<EmployeeTimeOff>
    {
        public override void Configure(EntityTypeBuilder<EmployeeTimeOff> builder)
        {
            base.Configure(builder);

            builder.ToTable("EmployeeTimeOffs");

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.StartDate)
                .IsRequired();

            builder.Property(x => x.EndDate)
                .IsRequired();

            builder.Property(x => x.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Reason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.ApprovedBy)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.HasOne(x => x.Employee)
                .WithMany(x => x.TimeOffs)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.StartDate);
            builder.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
        }
    }
}