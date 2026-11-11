using AppointMan.Domain.Enums;
using AppointMan.Domain.Events.User;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public PhoneNumber PhoneNumber { get; private set; }
        public string PasswordHash { get; private set; }
        public EnumUserRole Role { get; private set; }
        public EnumAccountStatus AccountStatus { get; private set; }
        public DateTime? DateOfBirth { get; private set; }
        public string? ProfilePictureUrl { get; private set; }
        public bool IsEmailVerified { get; private set; }
        public bool IsPhoneVerified { get; private set; }
        public DateTime? LastLoginDate { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiryTime { get; private set; }

        // Konum: kullanicinin kayitli oldugu il / ilce.
        // Arama kriteri bos geldiginde varsayilan konum olarak kullanilir.
        public int? CityId { get; private set; }
        public int? DistrictId { get; private set; }

        // Navigation Properties
        private readonly List<Appointment> _appointments = new();
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        private readonly List<Review> _reviews = new();
        public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

        private readonly List<Business> _ownedBusinesses = new();
        public IReadOnlyCollection<Business> OwnedBusinesses => _ownedBusinesses.AsReadOnly();

        private readonly List<Favorite> _favorites = new();
        public IReadOnlyCollection<Favorite> Favorites => _favorites.AsReadOnly();

        private readonly List<Notification> _notifications = new();
        public IReadOnlyCollection<Notification> Notifications => _notifications.AsReadOnly();

        private readonly List<Employee> _employees = new();
        public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

        private readonly List<BlacklistEntry> _blacklistEntries = new();
        public IReadOnlyCollection<BlacklistEntry> BlacklistEntries => _blacklistEntries.AsReadOnly();

        private User() { } // EF Core için

        public User(string firstName, string lastName, Email email, PhoneNumber phoneNumber, EnumUserRole role)
            : this(Guid.NewGuid(), firstName, lastName, email, phoneNumber, role)
        {
        }

        public User(Guid id, string firstName, string lastName, Email email, PhoneNumber phoneNumber, EnumUserRole role)
            : base(id)
        {
            SetFirstName(firstName);
            SetLastName(lastName);
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
            Role = role;
            AccountStatus = EnumAccountStatus.Active;
            IsEmailVerified = false;
            IsPhoneVerified = false;

            AddDomainEvent(new UserRegisteredEvent(Id, Email, $"{FirstName} {LastName}", Role));
        }

        public void SetFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("İsim boş olamaz.");

            if (firstName.Length < 2 || firstName.Length > 50)
                throw new DomainException("İsim 2-50 karakter arasında olmalıdır.");

            var oldFirstName = FirstName;
            FirstName = firstName;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(oldFirstName) && oldFirstName != firstName)
            {
                var updatedEvent = new UserProfileUpdatedEvent(Id, Email, $"{FirstName} {LastName}");
                updatedEvent.AddChange("FirstName", oldFirstName, firstName);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetLastName(string lastName)
        {
            if (string.IsNullOrWhiteSpace(lastName))
                throw new DomainException("Soyisim boş olamaz.");

            if (lastName.Length < 2 || lastName.Length > 50)
                throw new DomainException("Soyisim 2-50 karakter arasında olmalıdır.");

            var oldLastName = LastName;
            LastName = lastName;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(oldLastName) && oldLastName != lastName)
            {
                var updatedEvent = new UserProfileUpdatedEvent(Id, Email, $"{FirstName} {LastName}");
                updatedEvent.AddChange("LastName", oldLastName, lastName);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new DomainException("Şifre hash'i boş olamaz.");

            PasswordHash = passwordHash;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserPasswordChangedEvent(Id, Email, $"{FirstName} {LastName}", "System"));
        }

        public void SetDateOfBirth(DateTime? dateOfBirth)
        {
            if (dateOfBirth.HasValue && dateOfBirth.Value >= DateTime.UtcNow)
                throw new DomainException("Dogum tarihi gelecek bir tarih olamaz.");

            if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.UtcNow.AddYears(-18))
                throw new DomainException("Yas 18'den buyuk olmalidir.");

            DateOfBirth = dateOfBirth;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePassword(string newPasswordHash, string? changedBy = null)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("Şifre hash'i boş olamaz.");

            PasswordHash = newPasswordHash;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserPasswordChangedEvent(Id, Email, $"{FirstName} {LastName}", changedBy ?? "User"));
        }

        public void VerifyEmail()
        {
            if (IsEmailVerified)
                return;

            IsEmailVerified = true;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserEmailVerifiedEvent(Id, Email, $"{FirstName} {LastName}"));
        }

        public void VerifyPhone()
        {
            if (IsPhoneVerified)
                return;

            IsPhoneVerified = true;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserPhoneVerifiedEvent(Id, Email, $"{FirstName} {LastName}", PhoneNumber.Value));
        }

        public void UpdateLastLogin()
        {
            LastLoginDate = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRefreshToken(string refreshToken, DateTime expiryTime)
        {
            RefreshToken = refreshToken;
            RefreshTokenExpiryTime = expiryTime;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProfile(string firstName, string lastName, DateTime? dateOfBirth, string? profilePictureUrl, string? updatedBy = null)
        {
            var oldFirstName = FirstName;
            var oldLastName = LastName;
            var oldDateOfBirth = DateOfBirth;
            var oldProfilePictureUrl = ProfilePictureUrl;

            SetFirstName(firstName);
            SetLastName(lastName);
            DateOfBirth = dateOfBirth;
            ProfilePictureUrl = profilePictureUrl;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new UserProfileUpdatedEvent(Id, Email, $"{FirstName} {LastName}", updatedBy);

            if (oldFirstName != firstName)
                updatedEvent.AddChange("FirstName", oldFirstName, firstName);
            if (oldLastName != lastName)
                updatedEvent.AddChange("LastName", oldLastName, lastName);
            if (oldDateOfBirth != dateOfBirth)
                updatedEvent.AddChange("DateOfBirth", oldDateOfBirth?.ToString("yyyy-MM-dd") ?? "null", dateOfBirth?.ToString("yyyy-MM-dd") ?? "null");
            if (oldProfilePictureUrl != profilePictureUrl)
                updatedEvent.AddChange("ProfilePictureUrl", oldProfilePictureUrl ?? "null", profilePictureUrl ?? "null");

            if (updatedEvent.Changes.Any())
                AddDomainEvent(updatedEvent);
        }

        /// <summary>
        /// Kullanicinin kayitli konumunu (il / ilce) gunceller.
        /// Arama sirasinda kriter bos geldiginde bu bilgi kullanilir.
        /// </summary>
        public void UpdateLocation(int? cityId, int? districtId, string? updatedBy = null)
        {
            if (cityId.HasValue && cityId.Value <= 0)
                throw new DomainException("Il ID gecersiz.");

            if (districtId.HasValue && districtId.Value <= 0)
                throw new DomainException("Ilce ID gecersiz.");

            var oldCityId = CityId;
            var oldDistrictId = DistrictId;

            CityId = cityId;
            DistrictId = districtId;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new UserProfileUpdatedEvent(Id, Email, $"{FirstName} {LastName}", updatedBy);

            if (oldCityId != cityId)
                updatedEvent.AddChange("CityId", oldCityId?.ToString() ?? "null", cityId?.ToString() ?? "null");
            if (oldDistrictId != districtId)
                updatedEvent.AddChange("DistrictId", oldDistrictId?.ToString() ?? "null", districtId?.ToString() ?? "null");

            if (updatedEvent.Changes.Any())
                AddDomainEvent(updatedEvent);
        }

        public void ChangeEmail(Email newEmail)
        {
            if (Email == newEmail)
                throw new DomainException("Yeni email mevcut email ile aynı olamaz.");

            var oldEmail = Email;
            Email = newEmail;
            IsEmailVerified = false;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new UserProfileUpdatedEvent(Id, newEmail, $"{FirstName} {LastName}");
            updatedEvent.AddChange("Email", oldEmail, newEmail);
            AddDomainEvent(updatedEvent);
        }

        public void ChangePhoneNumber(PhoneNumber newPhoneNumber)
        {
            if (PhoneNumber == newPhoneNumber)
                throw new DomainException("Yeni telefon numarası mevcut numara ile aynı olamaz.");

            var oldPhoneNumber = PhoneNumber;
            PhoneNumber = newPhoneNumber;
            IsPhoneVerified = false;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new UserProfileUpdatedEvent(Id, Email, $"{FirstName} {LastName}");
            updatedEvent.AddChange("PhoneNumber", oldPhoneNumber.Value, newPhoneNumber.Value);
            AddDomainEvent(updatedEvent);
        }

        public void LockAccount(string? reason = null, string? lockedBy = null)
        {
            AccountStatus = EnumAccountStatus.Locked;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserAccountLockedEvent(Id, Email, $"{FirstName} {LastName}", reason, lockedBy));
        }

        public void UnlockAccount(string? unlockedBy = null)
        {
            AccountStatus = EnumAccountStatus.Active;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserAccountUnlockedEvent(Id, Email, $"{FirstName} {LastName}", unlockedBy));
        }

        public void AddAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            if (appointment.UserId != Id)
                throw new DomainException("Randevu bu kullanıcıya ait değil.");

            _appointments.Add(appointment);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddReview(Review review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (review.UserId != Id)
                throw new DomainException("Yorum bu kullanıcıya ait değil.");

            _reviews.Add(review);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddFavorite(Favorite favorite)
        {
            if (favorite == null)
                throw new ArgumentNullException(nameof(favorite));

            if (favorite.UserId != Id)
                throw new DomainException("Favori bu kullanıcıya ait değil.");

            if (_favorites.Any(f => f.BusinessId == favorite.BusinessId && !f.IsDeleted))
                throw new DomainException("Bu işletme zaten favorilerde.");

            _favorites.Add(favorite);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveFavorite(Guid businessId)
        {
            var favorite = _favorites.FirstOrDefault(f => f.BusinessId == businessId && !f.IsDeleted);
            if (favorite == null)
                throw new DomainException("Bu işletme favorilerde bulunamadı.");

            _favorites.Remove(favorite);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsFavorite(Guid businessId)
        {
            return _favorites.Any(f => f.BusinessId == businessId && !f.IsDeleted);
        }

        public void AddNotification(Notification notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (notification.UserId != Id)
                throw new DomainException("Bildirim bu kullanıcıya ait değil.");

            _notifications.Add(notification);
            UpdatedAt = DateTime.UtcNow;
        }

        public IReadOnlyCollection<Notification> GetUnreadNotifications()
        {
            return _notifications.Where(n => !n.IsRead && !n.IsDeleted).ToList().AsReadOnly();
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }
}