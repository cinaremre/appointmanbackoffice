namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts
{
    /// <summary>Tek bir calisan/saat cakismasinin ozeti.</summary>
    public class AdminAppointmentConflictDto
    {
        public Guid BusinessId { get; set; }

        /// <summary>Cakisan randevularin atandigi calisan.</summary>
        public Guid EmployeeId { get; set; }

        /// <summary>Cakisan randevu saati (UTC).</summary>
        public DateTime AppointmentDate { get; set; }

        /// <summary>Ayni calisana ayni saate dusen aktif randevu sayisi.</summary>
        public int Count { get; set; }

        /// <summary>Cakisan randevularin kimlikleri.</summary>
        public IReadOnlyList<Guid> AppointmentIds { get; set; } = Array.Empty<Guid>();
    }
}
