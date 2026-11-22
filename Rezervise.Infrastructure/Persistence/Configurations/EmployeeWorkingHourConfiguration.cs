using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class EmployeeWorkingHourConfiguration : BaseConfiguration<EmployeeWorkingHour>
    {
        public override void Configure(EntityTypeBuilder<EmployeeWorkingHour> builder)
        {
            base.Configure(builder);

            builder.ToTable("EmployeeWorkingHours");

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.DayOfWeek)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.StartTime)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired();

            builder.Property(x => x.BreakStartTime)
                .IsRequired(false);

            builder.Property(x => x.BreakEndTime)
                .IsRequired(false);

            builder.Property(x => x.IsClosed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(x => x.Employee)
                .WithMany(x => x.WorkingHours)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => new { x.EmployeeId, x.DayOfWeek }).IsUnique();
        }
    }
}