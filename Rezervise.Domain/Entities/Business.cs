using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Business;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Entities
{
    public class Business : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public EnumBusinessType BusinessType { get; private set; }
        public Address Address { get; private set; }
        public PhoneNumber PhoneNumber { get; private set; }
        public Email Email { get; private set; }
        public string? WebsiteUrl { get; private set; }
        public string? LogoUrl { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsVerified { get; private set; }

        /// <summary>
        /// Backoffice onayı: mobil üzerinden yeni kaydolan işletmeler bu bayrak
        /// false ile başlar ve arama sonuçlarında görünmez; backoffice onayı
        /// sonrası true olur ve bildirim gönderilir.
        /// </summary>
        public bool IsApproved { get; private set; }

        /// <summary>Backoffice onayının verildiği an (UTC); onaylanmamışsa null.</summary>
        public DateTime? ApprovedAt { get; private set; }
        public decimal? Rating { get; private set; }
        public int TotalReviews { get; private set; }
        public Guid OwnerId { get; private set; }

        public Guid? CategoryId { get; private set; }
        public BusinessCategory? Category { get; private set; }

        public User Owner { get; private set; }

        private readonly List<Service> _services = new();
        public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

        private readonly List<WorkingHour> _workingHours = new();
        public IReadOnlyCollection<WorkingHour> WorkingHours => _workingHours.AsReadOnly();

        private readonly List<Appointment> _appointments = new();
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        private readonly List<Favorite> _favorites = new();
        public IReadOnlyCollection<Favorite> Favorites => _favorites.AsReadOnly();

        private readonly List<Review> _reviews = new();
        public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

        private readonly List<Employee> _employees = new();
        public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

        private readonly List<BusinessServiceCategory> _serviceCategories = new();
        public IReadOnlyCollection<BusinessServiceCategory> ServiceCategories => _serviceCategories.AsReadOnly();

        private readonly List<BlacklistEntry> _blacklistEntries = new();
        public IReadOnlyCollection<BlacklistEntry> BlacklistEntries => _blacklistEntries.AsReadOnly();

        private Business() { }

        public Business(string name, EnumBusinessType businessType, Address address,
                PhoneNumber phoneNumber, Email email, Guid ownerId)
            : this(Guid.NewGuid(), name, businessType, address, phoneNumber, email, ownerId)
        {
        }

        public Business(Guid id, string name, EnumBusinessType businessType, Address address,
               PhoneNumber phoneNumber, Email email, Guid ownerId)
            : base(id)
        {
            SetName(name);
            BusinessType = businessType;
            Address = address ?? throw new ArgumentNullException(nameof(address));
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            OwnerId = ownerId;
            IsActive = true;
            IsVerified = false;
            IsApproved = false;
            ApprovedAt = null;
            Rating = 0;
            TotalReviews = 0;

            AddDomainEvent(new BusinessCreatedEvent(Id, Name, OwnerId, BusinessType, email, Address, PhoneNumber, Email));
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("İşletme adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("İşletme adı 2-100 karakter arasında olmalıdır.");

            var oldName = Name;
            Name = name;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(oldName) && oldName != name)
            {
                var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
                updatedEvent.AddChange("Name", oldName, name);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Açiklama 500 karakterden uzun olamaz.");

            var oldDescription = Description;
            Description = description;
            UpdatedAt = DateTime.UtcNow;

            if (oldDescription != description)
            {
                var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
                updatedEvent.AddChange("Description", oldDescription ?? string.Empty, description ?? string.Empty);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetCategory(BusinessCategory? category)
        {
            var oldCategoryId = CategoryId;
            Category = category;
            CategoryId = category?.Id;
            UpdatedAt = DateTime.UtcNow;

            if (oldCategoryId != CategoryId)
            {
                var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
                updatedEvent.AddChange("CategoryId", oldCategoryId?.ToString() ?? "null", CategoryId?.ToString() ?? "null");
                AddDomainEvent(updatedEvent);
            }
        }

        public void UpdateContactInfo(PhoneNumber phoneNumber, Email email, string? websiteUrl)
        {
            var oldPhoneNumber = PhoneNumber;
            var oldEmail = Email;
            var oldWebsiteUrl = WebsiteUrl;

            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            WebsiteUrl = websiteUrl;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
            if (oldPhoneNumber != phoneNumber)
                updatedEvent.AddChange("PhoneNumber", oldPhoneNumber, phoneNumber);
            if (oldEmail != email)
                updatedEvent.AddChange("Email", oldEmail, email);
            if (oldWebsiteUrl != websiteUrl)
                updatedEvent.AddChange("WebsiteUrl", oldWebsiteUrl ?? string.Empty, websiteUrl ?? string.Empty);

            if (updatedEvent.Changes.Any())
                AddDomainEvent(updatedEvent);
        }

        public void UpdateAddress(Address address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            var oldAddress = Address;
            Address = address;
            UpdatedAt = DateTime.UtcNow;

            if (oldAddress != address)
            {
                var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
                updatedEvent.AddChange(
                    "Address",
                    $"CityId: {oldAddress?.CityId}, DistrictId: {oldAddress?.DistrictId}",
                    $"CityId: {address.CityId}, DistrictId: {address.DistrictId}");
                AddDomainEvent(updatedEvent);
            }
        }

        public void UpdateLogo(string? logoUrl)
        {
            var oldLogoUrl = LogoUrl;
            LogoUrl = logoUrl;
            UpdatedAt = DateTime.UtcNow;

            if (oldLogoUrl != logoUrl)
            {
                var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
                updatedEvent.AddChange("LogoUrl", oldLogoUrl ?? string.Empty, logoUrl ?? string.Empty);
                AddDomainEvent(updatedEvent);
            }
        }

        public void Activate(string? activatedBy = null)
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new BusinessActivatedEvent(Id, Name, OwnerId, BusinessType, activatedBy));
        }

        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new BusinessDeactivatedEvent(Id, Name, OwnerId, BusinessType, deactivatedBy, reason));
        }

        public void Verify(string? verifiedBy = null)
        {
            if (IsVerified)
                return;

            IsVerified = true;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new BusinessVerifiedEvent(Id, Name, OwnerId, BusinessType, verifiedBy));
        }

        /// <summary>
        /// Backoffice onayı: işletmeyi onaylar. Bildirim (owner'a
        /// "hoşgeldiniz" mesajı) uygulama katmanında bu metoddan sonra
        /// oluşturulur. Zaten onaylıysa idempotenttir.
        /// </summary>
        public void Approve()
        {
            if (IsApproved)
                return;

            IsApproved = true;
            ApprovedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeOwner(Guid newOwnerId, string? changedBy = null)
        {
            if (newOwnerId == Guid.Empty)
                throw new ArgumentException("Yeni sahip ID boş olamaz.", nameof(newOwnerId));

            if (OwnerId == newOwnerId)
                throw new DomainException("İşletme zaten bu kullanıcıya ait.");

            var oldOwnerId = OwnerId;
            OwnerId = newOwnerId;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BusinessOwnerChangedEvent(Id, Name, oldOwnerId, newOwnerId, BusinessType, changedBy));
        }

        public void UpdateBusinessType(EnumBusinessType newBusinessType, string? changedBy = null)
        {
            if (BusinessType == newBusinessType)
                return;

            var oldBusinessType = BusinessType;
            BusinessType = newBusinessType;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BusinessCategoryChangedEvent(Id, Name, OwnerId, oldBusinessType, newBusinessType, changedBy));
        }

        public void Suspend(string? suspendedBy = null, string? reason = null, DateTime? suspendedUntil = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new BusinessSuspendedEvent(Id, Name, OwnerId, BusinessType, suspendedBy, reason, suspendedUntil));
        }

        public void Close(string? closedBy = null, string? reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new BusinessClosedEvent(Id, Name, OwnerId, BusinessType, closedBy, reason));
        }

        // ============= BLACKLIST METHODS =============

        public void AddBlacklistEntry(BlacklistEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (entry.BusinessId != Id)
                throw new DomainException("Kara liste kaydı bu işletmeye ait değil.");

            if (_blacklistEntries.Any(b => b.UserId == entry.UserId && !b.IsDeleted))
                throw new DomainException("Bu kullanıcı zaten kara listede.");

            _blacklistEntries.Add(entry);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsUserBlacklisted(Guid userId)
        {
            return _blacklistEntries.Any(b => b.UserId == userId && !b.IsDeleted);
        }

        public BlacklistEntry? GetActiveBlacklistEntry(Guid userId)
        {
            return _blacklistEntries.FirstOrDefault(b => b.UserId == userId && !b.IsDeleted);
        }

        public IReadOnlyCollection<BlacklistEntry> GetActiveBlacklistEntries()
        {
            return _blacklistEntries
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BlacklistedAt)
                .ToList()
                .AsReadOnly();
        }

        // ============= SERVICE CATEGORY METHODS =============

        public void AddServiceCategory(ServiceCategory serviceCategory)
        {
            if (serviceCategory == null)
                throw new ArgumentNullException(nameof(serviceCategory));

            if (!serviceCategory.IsRoot())
                throw new DomainException("İşletmeye sadece root kategori eklenebilir.");

            if (_serviceCategories.Any(sc => sc.ServiceCategoryId == serviceCategory.Id && !sc.IsDeleted))
                throw new DomainException("Bu kategori zaten işletmeye eklenmiş.");

            var businessServiceCategory = new BusinessServiceCategory(Id, serviceCategory.Id);
            _serviceCategories.Add(businessServiceCategory);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveServiceCategory(ServiceCategory serviceCategory)
        {
            if (serviceCategory == null)
                throw new ArgumentNullException(nameof(serviceCategory));

            var businessServiceCategory = _serviceCategories
                .FirstOrDefault(sc => sc.ServiceCategoryId == serviceCategory.Id && !sc.IsDeleted);

            if (businessServiceCategory is null)
                throw new DomainException("Bu kategori işletmede bulunamadı.");

            _serviceCategories.Remove(businessServiceCategory);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool HasServiceCategory(Guid serviceCategoryId)
        {
            return _serviceCategories.Any(sc => sc.ServiceCategoryId == serviceCategoryId && !sc.IsDeleted);
        }

        public bool CanProvideServiceCategory(Guid serviceCategoryRootId)
        {
            return _serviceCategories.Any(sc => sc.ServiceCategoryId == serviceCategoryRootId && !sc.IsDeleted);
        }

        // ============= EMPLOYEE METHODS =============

        public void AddEmployee(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            if (employee.BusinessId != Id)
                throw new DomainException("Çalisan bu işletmeye ait değil.");

            if (_employees.Any(e => e.GetFullName() == employee.GetFullName() && !e.IsDeleted))
                throw new DomainException("Bu isimde bir çalisan zaten mevcut.");

            _employees.Add(employee);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveEmployee(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            if (!_employees.Contains(employee))
                throw new DomainException("Bu çalisan işletmede bulunamadı.");

            _employees.Remove(employee);
            UpdatedAt = DateTime.UtcNow;
        }

        public Employee? GetEmployeeById(Guid employeeId)
        {
            return _employees.FirstOrDefault(e => e.Id == employeeId && !e.IsDeleted);
        }

        public IReadOnlyCollection<Employee> GetActiveEmployees()
        {
            return _employees
                .Where(e => e.IsActive && !e.IsDeleted)
                .OrderBy(e => e.DisplayOrder)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyCollection<Employee> GetAvailableEmployeesForService(Guid serviceId)
        {
            return _employees
                .Where(e => e.IsActive &&
                            e.IsAvailableForBooking &&
                            e.CanProvideService(serviceId) &&
                            !e.IsDeleted)
                .OrderBy(e => e.DisplayOrder)
                .ToList()
                .AsReadOnly();
        }

        // ============= SERVICE METHODS =============

        public void AddService(Service service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (service.BusinessId != Id)
                throw new DomainException("Hizmet bu işletmeye ait değil.");

            if (_services.Any(s => s.Name == service.Name && !s.IsDeleted))
                throw new DomainException("Bu isimde bir hizmet zaten mevcut.");

            var serviceCategoryRoot = service.ServiceCategory?.GetRootCategory();
            if (serviceCategoryRoot != null && !HasServiceCategory(serviceCategoryRoot.Id))
                throw new DomainException("Hizmetin kategorisi işletmeye ekli değil.");

            _services.Add(service);
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
            updatedEvent.AddChange("ServiceAdded", service.Name, $"Hizmet eklendi: {service.Name}");
            AddDomainEvent(updatedEvent);
        }

        public void RemoveService(Service service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            if (!_services.Contains(service))
                throw new DomainException("Bu hizmet işletmede bulunamadı.");

            _services.Remove(service);
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
            updatedEvent.AddChange("ServiceRemoved", service.Name, $"Hizmet silindi: {service.Name}");
            AddDomainEvent(updatedEvent);
        }

        public Service? GetServiceById(Guid serviceId)
        {
            return _services.FirstOrDefault(s => s.Id == serviceId && !s.IsDeleted);
        }

        public IReadOnlyCollection<Service> GetActiveServices()
        {
            return _services.Where(s => s.IsActive && !s.IsDeleted).ToList().AsReadOnly();
        }

        // ============= WORKING HOUR METHODS =============

        public void AddWorkingHour(WorkingHour workingHour)
        {
            if (workingHour == null)
                throw new ArgumentNullException(nameof(workingHour));

            if (workingHour.BusinessId != Id)
                throw new DomainException("Çalisma saati bu işletmeye ait değil.");

            if (_workingHours.Any(w => w.DayOfWeek == workingHour.DayOfWeek && !w.IsDeleted))
                throw new DomainException($"{workingHour.DayOfWeek} günü için zaten çalisma saati tanımlı.");

            _workingHours.Add(workingHour);
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
            updatedEvent.AddChange("WorkingHourAdded", workingHour.DayOfWeek, $"{workingHour.DayOfWeek}: {workingHour.StartTime}-{workingHour.EndTime}");
            AddDomainEvent(updatedEvent);
        }

        public void RemoveWorkingHour(WorkingHour workingHour)
        {
            if (workingHour == null)
                throw new ArgumentNullException(nameof(workingHour));

            if (!_workingHours.Contains(workingHour))
                throw new DomainException("Bu çalisma saati işletmede bulunamadı.");

            _workingHours.Remove(workingHour);
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new BusinessUpdatedEvent(Id, Name, OwnerId, BusinessType);
            updatedEvent.AddChange("WorkingHourRemoved", workingHour.DayOfWeek, $"{workingHour.DayOfWeek} çalisma saati silindi");
            AddDomainEvent(updatedEvent);
        }

        public WorkingHour? GetWorkingHourByDay(DayOfWeek day)
        {
            return _workingHours.FirstOrDefault(w => w.DayOfWeek == day && !w.IsDeleted);
        }

        public bool IsOpenOn(DateTime dateTime)
        {
            var workingHour = GetWorkingHourByDay(dateTime.DayOfWeek);
            if (workingHour == null || workingHour.IsClosed)
                return false;

            return workingHour.IsWithinWorkingHours(dateTime);
        }

        /// <summary>
        /// Aralığın tamamının o günün mesai saatleri içinde kaldığını kontrol eder.
        /// Nokta kontrolü yerine kullanılır; aralık yerel duvar saatinde verilir.
        /// </summary>
        public bool IsOpenDuring(DateTime start, DateTime end)
        {
            var workingHour = GetWorkingHourByDay(start.DayOfWeek);
            if (workingHour == null || workingHour.IsClosed)
                return false;

            return workingHour.IsWithinWorkingHours(start, end);
        }

        // ============= RATING METHODS =============

        public void UpdateRating(decimal newRating)
        {
            if (newRating < 0 || newRating > 5)
                throw new DomainException("Puan 0-5 arasında olmalıdır.");

            var oldRating = Rating ?? 0;
            Rating = Math.Round(newRating, 1);
            UpdatedAt = DateTime.UtcNow;

            if (oldRating != Rating)
            {
                AddDomainEvent(new BusinessRatingUpdatedEvent(Id, Name, OwnerId, BusinessType, oldRating, Rating.Value));
            }
        }

        public void IncrementTotalReviews()
        {
            TotalReviews++;
            UpdatedAt = DateTime.UtcNow;
        }

        public void DecrementTotalReviews()
        {
            if (TotalReviews > 0)
            {
                TotalReviews--;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        // ============= FAVORITE METHODS =============

        public int GetFavoriteCount()
        {
            return _favorites.Count;
        }

        public bool IsFavoritedBy(Guid userId)
        {
            return _favorites.Any(f => f.UserId == userId && !f.IsDeleted);
        }

        public void AddFavorite(Favorite favorite)
        {
            if (favorite == null)
                throw new ArgumentNullException(nameof(favorite));

            if (favorite.BusinessId != Id)
                throw new DomainException("Favori bu işletmeye ait değil.");

            if (_favorites.Any(f => f.UserId == favorite.UserId && !f.IsDeleted))
                throw new DomainException("Bu işletme zaten favorilerde.");

            _favorites.Add(favorite);
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BusinessFavoriteAddedEvent(Id, Name, OwnerId, BusinessType, favorite.UserId));
        }

        public void RemoveFavorite(Favorite favorite)
        {
            if (favorite == null)
                throw new ArgumentNullException(nameof(favorite));

            if (!_favorites.Contains(favorite))
                throw new DomainException("Bu favori bulunamadı.");

            _favorites.Remove(favorite);
            UpdatedAt = DateTime.UtcNow;
        }

        // ============= APPOINTMENT METHODS =============

        public void AddAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            if (appointment.BusinessId != Id)
                throw new DomainException("Randevu bu işletmeye ait değil.");

            _appointments.Add(appointment);
            UpdatedAt = DateTime.UtcNow;
        }

        public IReadOnlyCollection<Appointment> GetUpcomingAppointments()
        {
            return _appointments
                .Where(a => a.AppointmentDate > DateTime.UtcNow &&
                           a.Status == EnumAppointmentStatus.Confirmed &&
                           !a.IsDeleted)
                .OrderBy(a => a.AppointmentDate)
                .ToList()
                .AsReadOnly();
        }

        // ============= REVIEW METHODS =============

        public void AddReview(Review review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (review.BusinessId != Id)
                throw new DomainException("Yorum bu işletmeye ait değil.");

            IncrementTotalReviews();
            AddDomainEvent(new BusinessReviewReceivedEvent(
                Id, Name, OwnerId, BusinessType,
                review.Id, review.Rating, review.Comment, review.UserId));
        }
    }
}