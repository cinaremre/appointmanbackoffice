using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Employee;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Entities
{
    public class Employee : BaseEntity
    {
        // Kisisel bilgiler
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string? Title { get; private set; }
        public string? Bio { get; private set; }
        public string? ProfilePictureUrl { get; private set; }

        // Iletisim
        public PhoneNumber PhoneNumber { get; private set; }
        public Email? Email { get; private set; }

        // Isletme iliskisi
        public Guid BusinessId { get; private set; }
        public Business Business { get; private set; }

        // Kullanici iliskisi (opsiyonel)
        public Guid? UserId { get; private set; }
        public User? User { get; private set; }

        // Durum
        public bool IsActive { get; private set; }
        public bool IsAvailableForBooking { get; private set; }
        public int DisplayOrder { get; private set; }

        // Navigation Properties
        private readonly List<EmployeeService> _employeeServices = new();
        public IReadOnlyCollection<EmployeeService> EmployeeServices => _employeeServices.AsReadOnly();

        private readonly List<EmployeeWorkingHour> _workingHours = new();
        public IReadOnlyCollection<EmployeeWorkingHour> WorkingHours => _workingHours.AsReadOnly();

        private readonly List<EmployeeTimeOff> _timeOffs = new();
        public IReadOnlyCollection<EmployeeTimeOff> TimeOffs => _timeOffs.AsReadOnly();

        private readonly List<Appointment> _appointments = new();
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        private Employee() { } // EF Core

        public Employee(string firstName, string lastName, PhoneNumber phoneNumber, Guid businessId)
            : this(Guid.NewGuid(), firstName, lastName, phoneNumber, businessId)
        {
        }

        public Employee(Guid id, string firstName, string lastName, PhoneNumber phoneNumber, Guid businessId)
            : base(id)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("Calisan adi bos olamaz.");

            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Calisan soyadi bos olamaz.");

            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            SetFirstName(firstName);
            SetLastName(lastName);
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            BusinessId = businessId;
            IsActive = true;
            IsAvailableForBooking = true;
            DisplayOrder = 0;

            AddDomainEvent(new EmployeeCreatedEvent(Id, BusinessId, GetFullName()));
        }

        public void SetFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("Calisan adi bos olamaz.");

            if (firstName.Length < 2 || firstName.Length > 50)
                throw new DomainException("Calisan adi 2-50 karakter arasinda olmalidir.");

            FirstName = firstName.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Calisan soyadi bos olamaz.");

            if (lastName.Length < 2 || lastName.Length > 50)
                throw new DomainException("Calisan soyadi 2-50 karakter arasinda olmalidir.");

            LastName = lastName.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetTitle(string? title)
        {
            if (title != null && title.Length > 100)
                throw new DomainException("Unvan 100 karakterden uzun olamaz.");

            Title = title;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetBio(string? bio)
        {
            if (bio != null && bio.Length > 500)
                throw new DomainException("Biyografi 500 karakterden uzun olamaz.");

            Bio = bio;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetProfilePicture(string? url)
        {
            ProfilePictureUrl = url;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetEmail(Email? email)
        {
            Email = email;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPhoneNumber(PhoneNumber phoneNumber)
        {
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetDisplayOrder(int order)
        {
            if (order < 0)
                throw new DomainException("Siralama numarasi negatif olamaz.");

            DisplayOrder = order;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new EmployeeActivatedEvent(Id, BusinessId, GetFullName()));
        }

        public void Deactivate(string? reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            IsAvailableForBooking = false;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new EmployeeDeactivatedEvent(Id, BusinessId, GetFullName(), reason));
        }

        public void MakeAvailableForBooking()
        {
            if (!IsActive)
                throw new DomainException("Pasif calisan randevu alamaz.");

            IsAvailableForBooking = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MakeUnavailableForBooking()
        {
            IsAvailableForBooking = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProfile(
            string firstName,
            string lastName,
            string? title,
            string? bio,
            string? profilePictureUrl)
        {
            SetFirstName(firstName);
            SetLastName(lastName);
            SetTitle(title);
            SetBio(bio);
            SetProfilePicture(profilePictureUrl);

            AddDomainEvent(new EmployeeUpdatedEvent(Id, BusinessId, GetFullName()));
        }

        // ===== USER ILISKISI =====

        public void LinkToUser(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            UserId = user.Id;
            User = user;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnlinkFromUser()
        {
            UserId = null;
            User = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsLinkedToUser(Guid userId)
        {
            return UserId.HasValue && UserId.Value == userId;
        }

        // ===== SERVICE METHODS =====

        public void AddService(EmployeeService employeeService)
        {
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));

            if (employeeService.EmployeeId != Id)
                throw new DomainException("Hizmet bu calisana ait degil.");

            if (_employeeServices.Any(es => es.ServiceId == employeeService.ServiceId && !es.IsDeleted))
                throw new DomainException("Bu hizmet zaten calisana atanmis.");

            _employeeServices.Add(employeeService);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveService(EmployeeService employeeService)
        {
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));

            if (!_employeeServices.Contains(employeeService))
                throw new DomainException("Bu hizmet calisanda bulunamadi.");

            _employeeServices.Remove(employeeService);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool CanProvideService(Guid serviceId)
        {
            return _employeeServices.Any(es => es.ServiceId == serviceId && !es.IsDeleted);
        }

        // ===== WORKING HOUR METHODS =====

        public void AddWorkingHour(EmployeeWorkingHour workingHour)
        {
            if (workingHour == null)
                throw new ArgumentNullException(nameof(workingHour));

            if (workingHour.EmployeeId != Id)
                throw new DomainException("Calisma saati bu calisana ait degil.");

            if (_workingHours.Any(w => w.DayOfWeek == workingHour.DayOfWeek && !w.IsDeleted))
                throw new DomainException($"{workingHour.DayOfWeek} gunu icin zaten calisma saati tanimli.");

            _workingHours.Add(workingHour);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveWorkingHour(EmployeeWorkingHour workingHour)
        {
            if (workingHour == null)
                throw new ArgumentNullException(nameof(workingHour));

            if (!_workingHours.Contains(workingHour))
                throw new DomainException("Bu calisma saati calisanda bulunamadi.");

            _workingHours.Remove(workingHour);
            UpdatedAt = DateTime.UtcNow;
        }

        public EmployeeWorkingHour? GetWorkingHourByDay(DayOfWeek day)
        {
            return _workingHours.FirstOrDefault(w => w.DayOfWeek == day && !w.IsDeleted);
        }

        public bool IsWorkingOn(DateTime dateTime)
        {
            var workingHour = GetWorkingHourByDay(dateTime.DayOfWeek);
            if (workingHour == null || workingHour.IsClosed)
                return false;

            // Mola ve onayli izin kontrolleri aralik bazli metodlarda yapilir.
            return workingHour.IsWithinWorkingHours(dateTime) &&
                   !workingHour.IsWithinBreakTime(dateTime);
        }

        /// <summary>
        /// Araligin tamaminin calisanin mesai saatlerinde ve mola disinda oldugunu kontrol eder.
        /// Onayli izin kontrolu ayri bir metottur (IsOnApprovedTimeOff).
        /// </summary>
        public bool IsWorkingDuring(DateTime start, DateTime end)
        {
            var workingHour = GetWorkingHourByDay(start.DayOfWeek);
            if (workingHour == null || workingHour.IsClosed)
                return false;

            return workingHour.IsWithinWorkingHours(start, end) &&
                   !workingHour.OverlapsBreak(start, end);
        }

        // ===== TIME OFF METHODS =====

        public void AddTimeOff(EmployeeTimeOff timeOff)
        {
            if (timeOff == null)
                throw new ArgumentNullException(nameof(timeOff));

            if (timeOff.EmployeeId != Id)
                throw new DomainException("Izin bu calisana ait degil.");

            if (_timeOffs.Any(t => t.StartDate < timeOff.EndDate && t.EndDate > timeOff.StartDate && !t.IsDeleted))
                throw new DomainException("Bu tarih araliginda zaten izin var.");

            _timeOffs.Add(timeOff);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveTimeOff(EmployeeTimeOff timeOff)
        {
            if (timeOff == null)
                throw new ArgumentNullException(nameof(timeOff));

            if (!_timeOffs.Contains(timeOff))
                throw new DomainException("Bu izin calisanda bulunamadi.");

            _timeOffs.Remove(timeOff);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsOnTimeOff(DateTime dateTime)
        {
            // Onaylanmamis (bekleyen/reddedilmis) izinler calisani mesaiten dusurmez.
            return _timeOffs.Any(t => t.IsApproved && !t.IsDeleted && t.IsDateWithinTimeOff(dateTime));
        }

        /// <summary>
        /// Onayli ve silinmemis izinlerin verilen aralikla kesisip kesmedigini kontrol eder.
        /// Aralik yerel duvar saatinde verilir.
        /// </summary>
        public bool IsOnApprovedTimeOff(DateTime start, DateTime end)
        {
            return _timeOffs.Any(t => t.IsApproved && !t.IsDeleted && t.OverlapsWith(start, end));
        }

        // ===== HELPER METHODS =====

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Title))
                return $"{GetFullName()} - {Title}";

            return GetFullName();
        }
    }
}