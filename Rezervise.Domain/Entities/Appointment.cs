using System;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Events.Appointment;

namespace Rezervise.Domain.Entities
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
                throw new ArgumentException("Çalisan ID boş olamaz.", nameof(employeeId));

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
                // Seed için geçmiş tarihe izin ver
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
        /// Randevu tarihini doğrular ve atar.
        /// Geçmiş tarih kontrolü için kullanılan zaman dışarıdan verilir;
        /// böylece aynı istek içindeki tüm kontroller aynı referans zamanı kullanır.
        /// </summary>
        /// <param name="appointmentDate">Atanacak randevu zamanı.</param>
        /// <param name="now">Referans alınacak güncel UTC zamanı.</param>
        /// <exception cref="DomainException">Tarih geçmiş, 5 dakikaya hizalı değil veya saat aralığı dışında ise.</exception>
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
        /// <param name="confirmedBy">Onaylayan kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        /// <summary>
        /// Geçerli durum geçislerinin tek kaynağı. Handler'lar ve arka plan
        /// servisleri aynı matrisi kullanır; dağınık if kontrolleri kaldırılmıştır.
        /// </summary>
        public static bool IsValidStatusTransition(EnumAppointmentStatus from, EnumAppointmentStatus to)
        {
            return (from, to) switch
            {
                // Pending ? Pending yalnızca yeniden planlamada kullanılır.
                (EnumAppointmentStatus.Pending, EnumAppointmentStatus.Pending) => true,
                (EnumAppointmentStatus.Pending, EnumAppointmentStatus.Confirmed) => true,
                (EnumAppointmentStatus.Pending, EnumAppointmentStatus.Cancelled) => true,
                (EnumAppointmentStatus.Pending, EnumAppointmentStatus.NoShow) => true,
                // Confirmed ? Pending yalnızca yeniden planlamada kullanılır.
                (EnumAppointmentStatus.Confirmed, EnumAppointmentStatus.Pending) => true,
                (EnumAppointmentStatus.Confirmed, EnumAppointmentStatus.Completed) => true,
                (EnumAppointmentStatus.Confirmed, EnumAppointmentStatus.Cancelled) => true,
                (EnumAppointmentStatus.Confirmed, EnumAppointmentStatus.NoShow) => true,
                _ => false
            };
        }

        /// <summary>
        /// Durum matrisini ve zaman kurallarını birlikte uygular.
        /// </summary>
        public bool CanTransitionTo(EnumAppointmentStatus target, DateTime? now = null)
        {
            if (!IsValidStatusTransition(Status, target))
                return false;

            var currentTime = now ?? DateTime.UtcNow;

            return target switch
            {
                EnumAppointmentStatus.Cancelled => AppointmentDate > currentTime,
                EnumAppointmentStatus.Completed => AppointmentDate <= currentTime,
                EnumAppointmentStatus.NoShow => AppointmentDate <= currentTime,
                EnumAppointmentStatus.Confirmed or EnumAppointmentStatus.Pending => true,
                _ => false
            };
        }

        public void Confirm(string? confirmedBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (!CanTransitionTo(EnumAppointmentStatus.Confirmed, currentTime))
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
        /// <param name="reason">İptal gerekçesi.</param>
        /// <param name="cancelledBy">İptal eden kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void Cancel(string? reason = null, string? cancelledBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (!IsValidStatusTransition(Status, EnumAppointmentStatus.Cancelled))
                throw new DomainException("Tamamlanmış, iptal edilmiş veya gelmedi olarak işaretlenmiş randevu iptal edilemez.");

            if (AppointmentDate < currentTime)
                throw new DomainException("Geçmis randevu iptal edilemez.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.Cancelled;
            CancellationReason = reason;
            CancelledAt = currentTime;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, cancelledBy, reason ?? "Kullanıcı tarafından iptal edildi", currentTime);
            AddDomainEvent(new AppointmentCancelledEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, AppointmentDate, reason, cancelledBy));
        }

        /// <summary>
        /// Onaylanmış randevuyu tamamlandı olarak işaretler.
        /// </summary>
        /// <param name="completedBy">Tamamlayan kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void Complete(string? completedBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (!IsValidStatusTransition(Status, EnumAppointmentStatus.Completed))
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
        /// Randevuyu yeni bir tarihe taşır ve durumu bekleyene çeker.
        /// </summary>
        /// <param name="newDate">Yeni randevu zamanı.</param>
        /// <param name="rescheduledBy">İşlemi yapan kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void Reschedule(DateTime newDate, string? rescheduledBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (!CanTransitionTo(EnumAppointmentStatus.Pending, currentTime))
                throw new DomainException("İptal edilmiş, tamamlanmış veya gelmedi olarak işaretlenmiş randevu yeniden planlanamaz.");

            var oldStatus = Status;
            var oldDate = AppointmentDate;

            SetAppointmentDate(newDate, currentTime);
            Status = EnumAppointmentStatus.Pending;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, rescheduledBy,
                      $"Randevu {oldDate:dd.MM.yyyy HH:mm} ? {newDate:dd.MM.yyyy HH:mm} tarihine yeniden planlandı",
                      currentTime);
            AddDomainEvent(new AppointmentRescheduledEvent(Id, UserId, BusinessId, ServiceId, EmployeeId, oldDate, newDate, rescheduledBy));
        }

        /// <summary>
        /// Randevunun çalışanını kalıcı olarak değiştirir. Aynı çalışan yeniden seçilirse
        /// hiçbir değişiklik yapılmaz ve yeni domain event üretilmez.
        /// </summary>
        /// <param name="employeeId">Yeni çalışan ID.</param>
        /// <param name="changedBy">Değişikliği yapan kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void ReassignEmployee(Guid employeeId, string? changedBy = null, DateTime? now = null)
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            if (EmployeeId == employeeId)
                return;

            var currentTime = now ?? DateTime.UtcNow;
            var oldEmployeeId = EmployeeId;

            EmployeeId = employeeId;
            UpdatedAt = currentTime;

            _histories.Add(new AppointmentHistory(
                Id,
                Status,
                Status,
                changedBy,
                $"Çalışan {oldEmployeeId} ? {employeeId} olarak değiştirildi",
                changedAt: currentTime));

            AddDomainEvent(new AppointmentEmployeeReassignedEvent(
                Id, UserId, BusinessId, ServiceId, oldEmployeeId, employeeId, AppointmentDate, changedBy));
        }

        /// <summary>Hatırlatma gönderildi olarak işaretler.</summary>
        /// <param name="now">İşlem zamanı (UTC).</param>
        public void MarkReminderSent(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            IsReminderSent = true;
            ReminderSentAt = currentTime;
            UpdatedAt = currentTime;
        }

        /// <summary>
        /// Müşterinin randevu üzerinde değişiklik yapabileceği en yakın süre (saat).
        /// İşletme sahibi ve randevunun çalışanı bu sınırlamaya tabi değildir.
        /// </summary>
        public const int CustomerChangeCutoffHours = Common.WorkflowPolicy.CustomerChangeCutoffHours;

        /// <summary>Randevu "gelmedi" olarak işaretlenmiş mi?</summary>
        public bool IsNoShow => Status == EnumAppointmentStatus.NoShow;

        /// <summary>
        /// Randevuya kalan süre müşteri değişiklik sınırının altında mi?
        /// Müşteri iptal/erteleme kararını son Anda vermemeli; işletme tarafı muaf.
        /// </summary>
        /// <param name="now">Referans alınacak güncel UTC zamanı.</param>
        /// <returns>Kalan süre <see cref="CustomerChangeCutoffHours"/> saatten az ise true.</returns>
        public bool IsWithinCustomerCutoff(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return AppointmentDate - currentTime < TimeSpan.FromHours(CustomerChangeCutoffHours);
        }

        /// <summary>
        /// Randevuyu "gelmedi" olarak işaretler. Yalnızca randevu saati geldikten
        /// sonra ve bekleyen/onaylanmış randevular için geçerlidir.
        /// </summary>
        /// <param name="changedBy">İşlemi yapan kullanıcı.</param>
        /// <param name="now">İşlem zamanı (UTC).</param>
        /// <exception cref="DomainException">Durum geçişi veya zaman kuralı geçersizse.</exception>
        public void MarkAsNoShow(string? changedBy = null, DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            if (!CanTransitionTo(EnumAppointmentStatus.NoShow, currentTime))
                throw new DomainException("Sadece geçmiş, bekleyen veya onaylanmış randevular gelmedi olarak işaretlenebilir.");

            var oldStatus = Status;
            Status = EnumAppointmentStatus.NoShow;
            UpdatedAt = currentTime;

            AddHistory(oldStatus, Status, changedBy, "Randevuya gelinmedi", currentTime);
        }

        /// <summary>Randevunun iptal edilebilir olup olmadığını belirler.</summary>
        /// <param name="now">Referans alınacak güncel UTC zamanı.</param>
        public bool CanBeCancelled(DateTime? now = null)
        {
            return CanTransitionTo(EnumAppointmentStatus.Cancelled, now);
        }

        /// <summary>Randevunun yaklaşan (onaylı ve gelecekte) olup olmadığını belirler.</summary>
        /// <param name="now">Referans alınacak güncel UTC zamanı.</param>
        public bool IsUpcoming(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return Status == EnumAppointmentStatus.Confirmed &&
                   AppointmentDate > currentTime;
        }

        /// <summary>Randevunun geçmişte kalıp kalmadığını belirler.</summary>
        /// <param name="now">Referans alınacak güncel UTC zamanı.</param>
        public bool IsPast(DateTime? now = null)
        {
            var currentTime = now ?? DateTime.UtcNow;

            return AppointmentDate < currentTime;
        }

        /// <summary>
        /// Verilen zaman aralığı ile çakışıp çakışmadığını belirler.
        /// Bitiş saati bilinmiyorsa varsayılan 30 dakika kullanılır.
        /// </summary>
        public bool OverlapsWith(DateTime start, DateTime end)
        {
            return OverlapsWith(start, end, 30);
        }

        /// <summary>
        /// Verilen zaman aralığı ile çakışıp çakışmadığını belirler.
        /// EndTime null ise fallbackDurationMinutes kullanılır; böylece çakışma
        /// penceresi tek kaynaktan (servis süresi) hesaplanır ve DB kısıtıyla
        /// aynı formülü paylaşır.
        /// </summary>
        /// <param name="start">Kontrol edilecek aralığın başlangıcı.</param>
        /// <param name="end">Kontrol edilecek aralığın bitişi.</param>
        /// <param name="fallbackDurationMinutes">EndTime null ise kullanılacak süre (dakika).</param>
        /// <returns>Aralıklar çakışıyorsa true.</returns>
        /// <exception cref="DomainException">Süre 0 veya küçükse.</exception>
        public bool OverlapsWith(DateTime start, DateTime end, int fallbackDurationMinutes)
        {
            if (fallbackDurationMinutes <= 0)
                throw new DomainException("Varsayılan süre 0'dan büyük olmalıdır.");

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