using Rezervise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class EmployeeConfiguration : BaseConfiguration<Employee>
    {
        public override void Configure(EntityTypeBuilder<Employee> builder)
        {
            base.Configure(builder);

            builder.ToTable("Employees");

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Title)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(x => x.Bio)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.ProfilePictureUrl)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasConversion(
                    v => v.Value,
                    v => new Domain.ValueObjects.PhoneNumber(v)
                );

            builder.Property(x => x.Email)
                .IsRequired(false)
                .HasMaxLength(255)
                .HasConversion(
                    v => v == null ? null : v.Value,
                    v => string.IsNullOrEmpty(v) ? null : new Domain.ValueObjects.Email(v)
                );

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.UserId)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.IsAvailableForBooking)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasOne(x => x.Business)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.EmployeeServices)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.WorkingHours)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.TimeOffs)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Appointments)
                .WithOne(x => x.Employee)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.IsActive);
            builder.HasIndex(x => x.IsAvailableForBooking);
            builder.HasIndex(x => new { x.BusinessId, x.DisplayOrder });
        }
    }
}