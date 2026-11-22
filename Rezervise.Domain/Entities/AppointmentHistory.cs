using Rezervise.Domain.Enums;

namespace Rezervise.Domain.Entities
{
    public class AppointmentHistory : BaseEntity
    {
        public Guid AppointmentId { get; private set; }
        public EnumAppointmentStatus OldStatus { get; private set; }
        public EnumAppointmentStatus NewStatus { get; private set; }
        public string? ChangeReason { get; private set; }
        public DateTime ChangedAt { get; private set; }
        public string? ChangedBy { get; private set; } // UserId veya "System"
        public string? IpAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public Dictionary<string, object>? AdditionalData { get; private set; }

        // Navigation Property
        public Appointment Appointment { get; private set; }

        private AppointmentHistory() { } // EF Core

        public AppointmentHistory(
     Guid appointmentId,
     EnumAppointmentStatus oldStatus,
     EnumAppointmentStatus newStatus,
     string? changedBy = null,
     string? changeReason = null,
     string? ipAddress = null,
     string? userAgent = null,
     DateTime? changedAt = null)
     : this(Guid.NewGuid(), appointmentId, oldStatus, newStatus, changedBy, changeReason, ipAddress, userAgent, changedAt)
        {
        }
        public AppointmentHistory(
    Guid id,
    Guid appointmentId,
    EnumAppointmentStatus oldStatus,
    EnumAppointmentStatus newStatus,
    string? changedBy = null,
    string? changeReason = null,
    string? ipAddress = null,
    string? userAgent = null,
    DateTime? changedAt = null)
    : base(id)
        {
            if (appointmentId == Guid.Empty)
                throw new ArgumentException("Randevu ID bos olamaz.", nameof(appointmentId));

            AppointmentId = appointmentId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ChangedBy = changedBy ?? "System";
            ChangeReason = changeReason;
            // Degisiklik zamani disaridan verilebilir; ayni istek icindeki
            // tum kayitlar ayni referans zamani paylasir.
            ChangedAt = changedAt ?? DateTime.UtcNow;
            IpAddress = ipAddress;
            UserAgent = userAgent;
            AdditionalData = new Dictionary<string, object>();
        }
        public void AddAdditionalData(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key bos olamaz.", nameof(key));

            AdditionalData ??= new Dictionary<string, object>();
            AdditionalData[key] = value;
            UpdatedAt = DateTime.UtcNow;
        }

        public string GetChangeSummary()
        {
            return $"{ChangedAt:dd.MM.yyyy HH:mm} - {OldStatus} ? {NewStatus}" +
                   $"{(string.IsNullOrEmpty(ChangeReason) ? "" : $" ({ChangeReason})")}";
        }
        public void SetChangedAtForSeed(DateTime changedAt)
        {
            ChangedAt = changedAt;
        }
    }
}