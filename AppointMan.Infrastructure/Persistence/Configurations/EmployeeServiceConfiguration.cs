using AppointMan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AppointMan.Infrastructure.Persistence.Configurations
{
    public class EmployeeServiceConfiguration : BaseConfiguration<EmployeeService>
    {
        public override void Configure(EntityTypeBuilder<EmployeeService> builder)
        {
            base.Configure(builder);

            builder.ToTable("EmployeeServices");

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.CustomPrice)
                .IsRequired(false)
                .HasPrecision(18, 2);

            builder.Property(x => x.CustomDurationInMinutes)
                .IsRequired(false);

            builder.Property(x => x.Note)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.HasOne(x => x.Employee)
                .WithMany(x => x.EmployeeServices)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Service)
                .WithMany(x => x.EmployeeServices)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);   // CASCADE -> RESTRICT

            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.ServiceId);
            builder.HasIndex(x => new { x.EmployeeId, x.ServiceId }).IsUnique();
        }
    }
}