using System;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Events.Appointment;

namespace AppointMan.Domain.Entities
{
    public class Appointment : BaseEntity
    {
        public DateTime AppointmentDate { get; private set; }
        public DateTime? EndTime { get; private set; }
        public EnumAppointmentStatus Status { get; private set; }
        public string? Notes { get; private set; }
        public string? CancellationReason { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public bool IsReminderSent { get; private set; }
        public DateTime? ReminderSentAt { get; private set; }

        // Foreign Keys
        public Guid UserId { get; private set; }
        public Guid BusinessId { get; private set; }
        public Guid ServiceId { get; private set; }
        public Guid EmployeeId { get; private set; }

        // Navigation Properties
        public User User { get; private set; }
        public Business Business { get; private set; }
        public Service Service { get; private set; }
        public Employee Employee { get; private set; }

        // History Navigation
        private readonly List<AppointmentHistory> _histories = new();
        public IReadOnlyCollection<AppointmentHistory> Histories => _histories.AsReadOnly();

        private Appointment() { } // EF Core

        public Appointment(Guid userId, Guid businessId, Guid serviceId, Guid employeeId,
                          DateTime appointmentDate, string? notes = null)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));

            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID boş olamaz.", nameof(serviceId));

            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            UserId = userId;
            BusinessId = businessId;
            ServiceId = serviceId;
            EmployeeId = employeeId;

            SetAppointmentDate(appointmentDate);
            SetNotes(notes);

            Status = EnumAppointmentStatus.Pending;
            IsReminderSent = false;

            AddDomainEvent(new AppointmentCreatedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, notes));
        }

        public void SetAppointmentDate(DateTime appointmentDate)
        {
            if (appointmentDate < DateTime.UtcNow)
                throw new DomainException("Randevu tarihi geçmiş bir zaman olamaz.");

            if (appointmentDate.Minute % 5 != 0)
                throw new DomainException("Randevu saati 5 dakikanın katı olmalıdır.");

            if (appointmentDate.Hour < 6 || appointmentDate.Hour > 23)
                throw new DomainException("Randevu saati 06:00-23:00 arasında olmalıdır.");

            AppointmentDate = appointmentDate;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetEndTime(DateTime endTime)
        {
            if (endTime <= AppointmentDate)
                throw new DomainException("Bitiş saati başlangıç saatinden sonra olmalıdır.");

            EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetNotes(string? notes)
        {
            if (notes != null && notes.Length > 500)
                throw new DomainException("Notlar 500 karakterden uzun olamaz.");

            Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Confirm(string? confirmedBy = null)
        {
            if (Status != EnumAppointmentStatus.Pending)
                throw new DomainException("Sadece bekleyen randevular onaylanabilir.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;

            AddHistory(oldStatus, Status, confirmedBy, "Randevu onaylandı");
            AddDomainEvent(new AppointmentConfirmedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, confirmedBy));
        }

        public void Cancel(string? reason = null, string? cancelledBy = null)
        {
            if (Status == EnumAppointmentStatus.Completed)
                throw new DomainException("Tamamlanmış randevu iptal edilemez.");

            if (Status == EnumAppointmentStatus.Cancelled)
                throw new DomainException("Randevu zaten iptal edilmiş.");

            if (AppointmentDate < DateTime.UtcNow)
                throw new DomainException("Geçmiş randevu iptal edilemez.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddHistory(oldStatus, Status, cancelledBy, reason ?? "Kullanıcı tarafından iptal edildi");
            AddDomainEvent(new AppointmentCancelledEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, reason, cancelledBy));
        }

        public void Complete(string? completedBy = null)
        {
            if (Status != EnumAppointmentStatus.Confirmed)
                throw new DomainException("Sadece onaylanmış randevular tamamlanabilir.");

            if (AppointmentDate > DateTime.UtcNow)
                throw new DomainException("Gelecek randevu tamamlanamaz.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddHistory(oldStatus, Status, completedBy, "Randevu tamamlandı");
            AddDomainEvent(new AppointmentCompletedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, completedBy));
        }

        public void Reschedule(DateTime newDate, string? rescheduledBy = null)
        {
            if (Status == EnumAppointmentStatus.Completed)
                throw new DomainException("Tamamlanmış randevu yeniden planlanamaz.");

            if (Status == EnumAppointmentStatus.Cancelled)
                throw new DomainException("İptal edilmiş randevu yeniden planlanamaz.");

            var oldStatus = Status;
            var oldDate = AppointmentDate;

            SetAppointmentDate(newDate);
            Status = EnumAppointmentStatus.Pending;
            UpdatedAt = DateTime.UtcNow;

            AddHistory(oldStatus, Status, rescheduledBy,
                      $"Randevu {oldDate:dd.MM.yyyy HH:mm} → {newDate:dd.MM.yyyy HH:mm} tarihine yeniden planlandı");
            AddDomainEvent(new AppointmentRescheduledEvent(Id, UserId, BusinessId, ServiceId,EmployeeId, oldDate, newDate, rescheduledBy));
        }

        public void MarkReminderSent()
        {
            IsReminderSent = true;
            ReminderSentAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool CanBeCancelled()
        {
            return Status != EnumAppointmentStatus.Completed &&
                   Status != EnumAppointmentStatus.Cancelled &&
                   AppointmentDate > DateTime.UtcNow;
        }

        public bool IsUpcoming()
        {
            return Status == EnumAppointmentStatus.Confirmed &&
                   AppointmentDate > DateTime.UtcNow;
        }

        public bool IsPast()
        {
            return AppointmentDate < DateTime.UtcNow;
        }

        public bool OverlapsWith(DateTime start, DateTime end)
        {
            var appointmentEnd = EndTime ?? AppointmentDate.AddMinutes(30);
            return AppointmentDate < end && appointmentEnd > start;
        }

        // History Methods
        private void AddHistory(EnumAppointmentStatus oldStatus, EnumAppointmentStatus newStatus,
                        string? changedBy = null, string? changeReason = null)
        {
            var history = new AppointmentHistory(
                Id,
                oldStatus,
                newStatus,
                changedBy ?? "System",
                changeReason);

            _histories.Add(history);
            AddDomainEvent(new AppointmentStatusChangedEvent(
                Id, UserId, BusinessId, EmployeeId, oldStatus, newStatus, changedBy, changeReason));
        }

        public IReadOnlyCollection<AppointmentHistory> GetHistory()
        {
            return _histories.OrderByDescending(h => h.ChangedAt).ToList().AsReadOnly();
        }

        public AppointmentHistory? GetLastChange()
        {
            return _histories.OrderByDescending(h => h.ChangedAt).FirstOrDefault();
        }

        public IReadOnlyCollection<AppointmentHistory> GetHistoryByStatus(EnumAppointmentStatus status)
        {
            return _histories
                .Where(h => h.NewStatus == status)
                .OrderByDescending(h => h.ChangedAt)
                .ToList()
                .AsReadOnly();
        }
    }
}