using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class AppointmentSeed
    {
        public static readonly Guid Apt1Id = Guid.Parse("e1000001-0000-0000-0000-000000000001");
        public static readonly Guid Apt2Id = Guid.Parse("e1000002-0000-0000-0000-000000000002");
        public static readonly Guid Apt3Id = Guid.Parse("e1000003-0000-0000-0000-000000000003");
        public static readonly Guid Apt4Id = Guid.Parse("e1000004-0000-0000-0000-000000000004");
        public static readonly Guid Apt5Id = Guid.Parse("e1000005-0000-0000-0000-000000000005");
        public static readonly Guid Apt6Id = Guid.Parse("e1000006-0000-0000-0000-000000000006");
        public static readonly Guid Apt7Id = Guid.Parse("e1000007-0000-0000-0000-000000000007");
        public static readonly Guid Apt8Id = Guid.Parse("e1000008-0000-0000-0000-000000000008");
        public static readonly Guid Apt9Id = Guid.Parse("e1000009-0000-0000-0000-000000000009");
        public static readonly Guid Apt10Id = Guid.Parse("e1000010-0000-0000-0000-000000000010");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Appointments.AnyAsync())
            {
                logger.LogInformation("Appointments zaten mevcut, atlanıyor.");
                return;
            }

            var today = DateTime.UtcNow.Date;
            var appointments = new List<Appointment>();

            // GECMIS - Tamamlanmış
            appointments.Add(CreateAppointment(Apt1Id,
                UserSeed.Customer1Id, BusinessSeed.Biz1Id, ServiceSeed.Svc1Id, EmployeeSeed.Emp1Id,
                today.AddDays(-10).AddHours(10), EnumAppointmentStatus.Completed, "Klasik kesim",
                today.AddDays(-10).AddHours(11)));

            appointments.Add(CreateAppointment(Apt2Id,
                UserSeed.Customer2Id, BusinessSeed.Biz2Id, ServiceSeed.Svc5Id, EmployeeSeed.Emp4Id,
                today.AddDays(-8).AddHours(14), EnumAppointmentStatus.Completed, null,
                today.AddDays(-8).AddHours(15)));

            appointments.Add(CreateAppointment(Apt3Id,
                UserSeed.Customer3Id, BusinessSeed.Biz5Id, ServiceSeed.Svc15Id, EmployeeSeed.Emp10Id,
                today.AddDays(-5).AddHours(11), EnumAppointmentStatus.Cancelled, null, null,
                "Hasta gelemedi"));

            appointments.Add(CreateAppointment(Apt4Id,
                UserSeed.Customer4Id, BusinessSeed.Biz3Id, ServiceSeed.Svc9Id, EmployeeSeed.Emp6Id,
                today.AddDays(-3).AddHours(15), EnumAppointmentStatus.Completed, "Küçük dövme",
                today.AddDays(-3).AddHours(16)));

            appointments.Add(CreateAppointment(Apt5Id,
                UserSeed.Customer5Id, BusinessSeed.Biz1Id, ServiceSeed.Svc1Id, EmployeeSeed.Emp3Id,
                today.AddDays(-2).AddHours(13), EnumAppointmentStatus.Completed, null,
                today.AddDays(-2).AddHours(14)));

            // GELECEK - Onaylanmış
            appointments.Add(CreateAppointment(Apt6Id,
                UserSeed.Customer1Id, BusinessSeed.Biz1Id, ServiceSeed.Svc1Id, EmployeeSeed.Emp1Id,
                today.AddDays(1).AddHours(10), EnumAppointmentStatus.Confirmed,
                "Özel kesim istiyorum", null));

            appointments.Add(CreateAppointment(Apt7Id,
                UserSeed.Customer2Id, BusinessSeed.Biz2Id, ServiceSeed.Svc5Id, EmployeeSeed.Emp4Id,
                today.AddDays(2).AddHours(14), EnumAppointmentStatus.Confirmed,
                "Balayage istiyorum", null));

            // GELECEK - Beklemede
            appointments.Add(CreateAppointment(Apt8Id,
                UserSeed.Customer3Id, BusinessSeed.Biz5Id, ServiceSeed.Svc16Id, EmployeeSeed.Emp11Id,
                today.AddDays(3).AddHours(11), EnumAppointmentStatus.Pending,
                "Dış ağrısı var", null));

            appointments.Add(CreateAppointment(Apt9Id,
                UserSeed.Customer4Id, BusinessSeed.Biz3Id, ServiceSeed.Svc9Id, EmployeeSeed.Emp6Id,
                today.AddDays(4).AddHours(16), EnumAppointmentStatus.Confirmed,
                "Küçük dövme - minimal tasarım", null));

            appointments.Add(CreateAppointment(Apt10Id,
                UserSeed.Customer5Id, BusinessSeed.Biz1Id, ServiceSeed.Svc2Id, EmployeeSeed.Emp2Id,
                today.AddDays(1).AddHours(15), EnumAppointmentStatus.Pending, null, null));

            await context.Appointments.AddRangeAsync(appointments);
            await context.SaveChangesAsync();

            logger.LogInformation("Appointments seed edildi: {Count} randevu", appointments.Count);
        }

        private static Appointment CreateAppointment(Guid id, Guid userId, Guid businessId,
    Guid serviceId, Guid employeeId, DateTime appointmentDate, EnumAppointmentStatus status,
    string? notes, DateTime? completedAt = null, string? cancellationReason = null)
        {
            // Seed için geçmiş tarihe izin ver
            var appointment = new Appointment(
                id, userId, businessId, serviceId, employeeId,
                appointmentDate, notes,false);

            var endTime = appointmentDate.AddMinutes(30);

            switch (status)
            {
                case EnumAppointmentStatus.Confirmed:
                    appointment.SetStatusForSeed(EnumAppointmentStatus.Confirmed, endTime: endTime);
                    break;

                case EnumAppointmentStatus.Completed:
                    appointment.SetStatusForSeed(EnumAppointmentStatus.Completed,
                        completedAt: completedAt ?? DateTime.UtcNow,
                        endTime: endTime);
                    break;

                case EnumAppointmentStatus.Cancelled:
                    appointment.SetStatusForSeed(EnumAppointmentStatus.Cancelled,
                        cancelledAt: DateTime.UtcNow,
                        cancellationReason: cancellationReason,
                        endTime: endTime);
                    break;

                case EnumAppointmentStatus.Pending:
                default:
                    appointment.SetStatusForSeed(EnumAppointmentStatus.Pending, endTime: endTime);
                    break;
            }

            return appointment;
        }
    }
}