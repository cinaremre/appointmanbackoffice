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

        public Appointment() { } // EF Core

        // ===== NORMAL CONSTRUCTOR (validasyonlu) =====
        public Appointment(Guid userId, Guid businessId, Guid serviceId, Guid employeeId,
                  DateTime appointmentDate, string? notes = null)
            : this(Guid.NewGuid(), userId, businessId, serviceId, employeeId, appointmentDate, notes, validateDate: true)
        {
        }

        // ===== ID'LI CONSTRUCTOR =====
        public Appointment(Guid id, Guid userId, Guid businessId, Guid serviceId, Guid employeeId,
                  DateTime appointmentDate, string? notes = null)
            : this(id, userId, businessId, serviceId, employeeId, appointmentDate, notes, validateDate: true)
        {
        }

        // ===== SEED ICIN CONSTRUCTOR (validasyonsuz) =====
        public Appointment(Guid id, Guid userId, Guid businessId, Guid serviceId, Guid employeeId,
                  DateTime appointmentDate, string? notes, bool validateDate)
            : base(id)
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

            if (validateDate)
            {
                SetAppointmentDate(appointmentDate);
            }
            else
            {
                // Seed icin gecmis tarihe izin ver
                AppointmentDate = appointmentDate;
                UpdatedAt = DateTime.UtcNow;
            }

            SetNotes(notes);

            Status = EnumAppointmentStatus.Pending;
            IsReminderSent = false;

            AddDomainEvent(new AppointmentCreatedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, notes));
        }

        public void SetAppointmentDate(DateTime appointmentDate)
        {
            SetAppointmentDate(appointmentDate, DateTime.UtcNow);
        }

        /// <summary>
        /// Randevu tarihini dogrular ve atar.
        /// Gecmis tarih kontrolu icin kullanilan zaman disaridan verilir;
        /// boylece ayni istek icindeki tum kontroller ayni referans zamani kullanir.
        /// </summary>
        /// <param name="appointmentDate">Atanacak randevu zamani.</param>
        /// <param name="now">Referans alinacak guncel UTC zamani.</param>
        /// <exception cref="DomainException">Tarih gecmis, 5 dakikaya hizali degil veya saat araligi disinda ise.</exception>
        public void SetAppointmentDate(DateTime appointmentDate, DateTime now)
        {
            if (appointmentDate < now)
                throw new DomainException("Appointment.DateInPast");

            if (appointmentDate.Minute % 5 != 0)
                throw new DomainException("Appointment.TimeStepFiveMinutes");

            if (appointmentDate.Hour < 6 || appointmentDate.Hour > 23)
                throw new DomainException("Appointment.TimeOutOfWindow");

            AppointmentDate = appointmentDate;
            UpdatedAt = now;
        }

        public void SetEndTime(DateTime endTime)
        {
            if (endTime <= AppointmentDate)
                throw new DomainException("Appointment.EndTimeAfterStart");

            EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetNotes(string? notes)
        {
            if (notes != null && notes.Length > 500)
                throw new DomainException("Validation.NotesMaxLength");

            Notes = notes;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Bekleyen randevuyu onaylar.
        /// </summary>
        /// <param name="confirmedBy">Onaylayan kullanici.</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Confirm(string? confirmedBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (Status != EnumAppointmentStatus.Pending)
                throw new DomainException("Sadece bekleyen randevular onaylanabilir.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Confirmed;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, confirmedBy, "Randevu onaylandı", currentTime);
            AddDomainEvent(new AppointmentConfirmedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, confirmedBy));
        }

        /// <summary>
        /// Randevuyu iptal eder.
        /// </summary>
        /// <param name="reason">Iptal gerekcesi.</param>
        /// <param name="cancelledBy">Iptal eden kullanici.</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Cancel(string? reason = null, string? cancelledBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (Status == EnumAppointmentStatus.Completed)
                throw new DomainException("Tamamlanmış randevu iptal edilemez.");

            if (Status == EnumAppointmentStatus.Cancelled)
                throw new DomainException("Randevu zaten iptal edilmiş.");

            if (AppointmentDate < currentTime)
                throw new DomainException("Geçmiş randevu iptal edilemez.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = currentTime;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, cancelledBy, reason ?? "Kullanıcı tarafından iptal edildi", currentTime);
            AddDomainEvent(new AppointmentCancelledEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, reason, cancelledBy));
        }

        /// <summary>
        /// Onaylanmis randevuyu tamamlandi olarak isaretler.
        /// </summary>
        /// <param name="completedBy">Tamamlayan kullanici.</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Complete(string? completedBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (Status != EnumAppointmentStatus.Confirmed)
                throw new DomainException("Sadece onaylanmış randevular tamamlanabilir.");

            if (AppointmentDate > currentTime)
                throw new DomainException("Gelecek randevu tamamlanamaz.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Completed;
            CompletedAt = currentTime;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, completedBy, "Randevu tamamlandı", currentTime);
            AddDomainEvent(new AppointmentCompletedEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, completedBy));
        }

        /// <summary>
        /// Randevuyu yeni bir tarihe tasir ve durumu bekleyene ceker.
        /// </summary>
        /// <param name="newDate">Yeni randevu zamani.</param>
        /// <param name="rescheduledBy">Islemi yapan kullanici.</param>
        /// <param name="now">Islem zamani (UTC).</param>
        public void Reschedule(DateTime newDate, string? rescheduledBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (Status == EnumAppointmentStatus.Completed)
                throw new DomainException("Tamamlanmış randevu yeniden planlanamaz.");

            if (Status == EnumAppointmentStatus.Cancelled)
                throw new DomainException("İptal edilmiş randevu yeniden planlanamaz.");

            var oldStatus = Status;
            var oldDate = AppointmentDate;

            SetAppointmentDate(newDate, currentTime);
            Status = EnumAppointmentStatus.Pending;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, rescheduledBy,
                      $"Randevu {oldDate:dd.MM.yyyy HH:mm} → {newDate:dd.MM.yyyy HH:mm} tarihine yeniden planlandı",
                      currentTime);
            AddDomainEvent(new AppointmentRescheduledEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, oldDate, newDate, rescheduledBy));
        }

        /// <summary>Hatirlatma gonderildi olarak isaretler.</summary>
        /// <param name="now">Islem zamani (UTC).</param>
        public void MarkReminderSent(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            IsReminderSent = true;
            ReminderSentAt = currentTime;
            UpdatedAt = currentTime;
        }

        /// <summary>Randevunun iptal edilebilir olup olmadigini belirler.</summary>
        /// <param name="now">Referans alinacak guncel UTC zamani.</param>
        public bool CanBeCancelled(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return Status != EnumAppointmentStatus.Completed &&
                   Status != EnumAppointmentStatus.Cancelled &&
                   AppointmentDate > currentTime;
        }

        /// <summary>Randevunun yaklasan (onayli ve gelecekte) olup olmadigini belirler.</summary>
        /// <param name="now">Referans alinacak guncel UTC zamani.</param>
        public bool IsUpcoming(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return Status == EnumAppointmentStatus.Confirmed &&
                   AppointmentDate > currentTime;
        }

        /// <summary>Randevunun gecmiste kalip kalmadigini belirler.</summary>
        /// <param name="now">Referans alinacak guncel UTC zamani.</param>
        public bool IsPast(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return AppointmentDate < currentTime;
        }

        /// <summary>
        /// Verilen zaman araligi ile cakisip cakismadigini belirler.
        /// Bitis saati bilinmiyorsa varsayilan 30 dakika kullanilir.
        /// </summary>
        public bool OverlapsWith(DateTime start, DateTime end)
        {
            return OverlapsWith(start, end, 30);
        }

        /// <summary>
        /// Verilen zaman araligi ile cakisip cakismadigini belirler.
        /// EndTime null ise fallbackDurationMinutes kullanilir; boylece cakisma
        /// penceresi tek kaynaktan (servis suresi) hesaplanir ve DB kisitiyla
        /// ayni formulu paylasir.
        /// </summary>
        /// <param name="start">Kontrol edilecek araligin baslangici.</param>
        /// <param name="end">Kontrol edilecek araligin bitisi.</param>
        /// <param name="fallbackDurationMinutes">EndTime null ise kullanilacak sure (dakika).</param>
        /// <returns>Araliklar cakisiyorsa true.</returns>
        /// <exception cref="DomainException">Sure 0 veya kucukse.</exception>
        public bool OverlapsWith(DateTime start, DateTime end, int fallbackDurationMinutes)
        {
            if (fallbackDurationMinutes <= 0)
                throw new DomainException("Varsayilan sure 0'dan buyuk olmalidir.");

            var appointmentEnd = EndTime ?? AppointmentDate.AddMinutes(fallbackDurationMinutes);
            return AppointmentDate < end && appointmentEnd > start;
        }

        // History Methods
        private void AddHistory(EnumAppointmentStatus oldStatus, EnumAppointmentStatus newStatus,
                        string? changedBy = null, string? changeReason = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            var history = new AppointmentHistory(
                Id,
                oldStatus,
                newStatus,
                changedBy ?? "System",
                changeReason,
                changedAt: currentTime);

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

        // ===== SEED ICIN =====
        public void SetStatusForSeed(
            EnumAppointmentStatus status,
            DateTime? completedAt = null,
            DateTime? cancelledAt = null,
            string? cancellationReason = null,
            DateTime? endTime = null)
        {
            Status = status;
            CompletedAt = completedAt;
            CancelledAt = cancelledAt;
            CancellationReason = cancellationReason;
            if (endTime.HasValue) EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}