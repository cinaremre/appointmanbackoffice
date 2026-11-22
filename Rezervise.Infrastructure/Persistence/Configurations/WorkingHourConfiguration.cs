using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class WorkingHourConfiguration : BaseConfiguration<WorkingHour>
    {
        public override void Configure(EntityTypeBuilder<WorkingHour> builder)
        {
            base.Configure(builder);

            builder.ToTable("WorkingHours");

            builder.Property(x => x.DayOfWeek)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.StartTime)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired();

            builder.Property(x => x.IsClosed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.HasOne(x => x.Business)
                .WithMany(x => x.WorkingHours)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => new { x.BusinessId, x.DayOfWeek }).IsUnique();
        }
    }
}