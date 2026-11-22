using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Rezervise.Infrastructure.Persistence.Configurations
{
    public class AppointmentConfiguration : BaseConfiguration<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);

            builder.ToTable("Appointments");

            builder.Property(x => x.AppointmentDate)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired(false);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(EnumAppointmentStatus.Pending);

            builder.Property(x => x.Notes)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.CancellationReason)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(x => x.CancelledAt)
                .IsRequired(false);

            builder.Property(x => x.CompletedAt)
                .IsRequired(false);

            builder.Property(x => x.IsReminderSent)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.ReminderSentAt)
                .IsRequired(false);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.ServiceId)
                .IsRequired();

            builder.Property(x => x.EmployeeId)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Business)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Service)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Employee)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Histories)
                .WithOne(x => x.Appointment)
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.ServiceId);
            builder.HasIndex(x => x.EmployeeId);
            builder.HasIndex(x => x.AppointmentDate);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => new { x.BusinessId, x.AppointmentDate });

            // Ayni calisanin ayni baslangic saatine iki aktif randevusu DB'de imkansiz.
            // Yalnizca iptal edilmemis (3 = Cancelled) ve silinmemis satirlar filtreye girer.
            // NOT: slot baslangiclari 5 dakikaya hizali oldugu icin (Minute % 5 == 0)
            // bu index pratikte etkilidir; farkli baslangicli aralik cakismasini
            // yakalamaz -> asil koruma uygulama tarafindaki aralik kilididir.
            builder.HasIndex(x => new { x.EmployeeId, x.AppointmentDate })
                .IsUnique()
                .HasFilter("[Status] <> 3 AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Appointments_EmployeeId_AppointmentDate_Active");

            // Cakisma sorgusu (EmployeeId + aralik) icin yardimci index.
            builder.HasIndex(x => new { x.EmployeeId, x.AppointmentDate, x.EndTime })
                .HasDatabaseName("IX_Appointments_EmployeeId_Date_End");
        }
    }
}