namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>Tek bir çalışan/saat çakışmasının özeti.</summary>
    public class AdminAppointmentConflictDto
    {
        public Guid BusinessId { get; set; }

        /// <summary>Çakışan randevuların atandığı çalışan.</summary>
        public Guid EmployeeId { get; set; }

        /// <summary>Çakışan randevu saati (UTC).</summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>Aynı çalışana aynı saate düşen aktif randevu sayısı.</summary>
        public int Count { get; set; }

        /// <summary>Çakışan randevuların kimlikleri.</summary>
        public IReadOnlyList<Guid> AppointmentIds { get; set; } = Array.Empty<Guid>();
    }
}
