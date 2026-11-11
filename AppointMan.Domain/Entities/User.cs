using AppointMan.Domain.Enums;
using AppointMan.Domain.Events.User;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Entities
{
    public class User : BaseEntity
    {
        /// <summary>Desteklenen dil kodlari.</summary>
        private static readonly string[] SupportedLanguages = { "tr", "en" };

        /// <summary>Varsayilan dil kodu.</summary>
        public const string DefaultLanguageCode = "tr";

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

        // Guvenlik: refresh token yalnizca hash'lenmis olarak saklanir; ham deger DB'de tutulmaz.
        public string? RefreshTokenHash { get; private set; }
        public string? PreviousRefreshTokenHash { get; private set; }
        public DateTime? RefreshTokenExpiresAtUtc { get; private set; }
        public DateTime? RefreshTokenCreatedAtUtc { get; private set; }
        public DateTime? RefreshTokenRevokedAtUtc { get; private set; }

        // Guvenlik: brute force korumasi icin basarisiz giris sayaci ve kilit bitis zamani.
        public int AccessFailedCount { get; private set; }
        public DateTime? LockoutEndUtc { get; private set; }

        /// <summary>Hesabin su anda kilitli olup olmadigini bildirir.</summary>
        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

        /// <summary>Son basarili giris zamani (UTC).</summary>
        public DateTime? LastFailedLoginUtc { get; private set; }

        // Konum: kullanicinin kayitli oldugu il / ilce.
        // Arama kriteri bos geldiginde varsayilan konum olarak kullanilir.
        public int? CityId { get; private set; }
        public int? DistrictId { get; private set; }

        // Dil tercihi: "tr" veya "en". Kullaniciya donen metinlerin kulturu
        // bu deger uzerinden belirlenir.
        public string LanguageCode { get; private set; }

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
            LanguageCode = DefaultLanguageCode;

            AddDomainEvent(new UserRegisteredEvent(Id, Email, $"{FirstName} {LastName}", Role));
        }

        public void SetFirstName(string firstName)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new DomainException("Validation.FirstNameRequired");

            if (firstName.Length < 2 || firstName.Length > 50)
                throw new DomainException("Validation.FirstNameLength");

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
                throw new DomainException("Validation.LastNameRequired");

            if (lastName.Length < 2 || lastName.Length > 50)
                throw new DomainException("Validation.LastNameLength");

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
                throw new DomainException("User.PasswordHashRequired");

            PasswordHash = passwordHash;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new UserPasswordChangedEvent(Id, Email, $"{FirstName} {LastName}", "System"));
        }

        public void SetDateOfBirth(DateTime? dateOfBirth)
        {
            if (dateOfBirth.HasValue && dateOfBirth.Value >= DateTime.UtcNow)
                throw new DomainException("User.DateOfBirthInFuture");

            if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.UtcNow.AddYears(-18))
                throw new DomainException("Validation.DateOfBirthMinAge");

            DateOfBirth = dateOfBirth;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePassword(string newPasswordHash, string? changedBy = null)
        {
            if (string.IsNullOrWhiteSpace(newPasswordHash))
                throw new DomainException("User.PasswordHashRequired");

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

        /// <summary>
        /// Yeni refresh token hash'ini kaydeder ve mevcut hash'i tekrar kullanim
        /// (reuse) tespiti icin onceki slota tasir.
        /// </summary>
        /// <param name="refreshTokenHash">Yeni refresh token'in SHA-256 hash'i.</param>
        /// <param name="expiresAtUtc">Yeni token'in mutlak bitis zamani (UTC).</param>
        /// <param name="issuedAtUtc">Token'in uretildigi zaman (UTC).</param>
        public void IssueRefreshToken(string refreshTokenHash, DateTime expiresAtUtc, DateTime issuedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenHash))
                throw new DomainException("User.RefreshTokenHashRequired");

            PreviousRefreshTokenHash = RefreshTokenHash;
            RefreshTokenHash = refreshTokenHash;
            RefreshTokenExpiresAtUtc = expiresAtUtc;
            RefreshTokenCreatedAtUtc = issuedAtUtc;
            RefreshTokenRevokedAtUtc = null;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Refresh token durumunu iptal eder. Cikis, parola degisimi ve
        /// token tekrar kullanimi tespitinde cagrilir.
        /// </summary>
        /// <param name="revokedAtUtc">Iptal zamani (UTC).</param>
        public void RevokeRefreshToken(DateTime revokedAtUtc)
        {
            RefreshTokenHash = null;
            PreviousRefreshTokenHash = null;
            RefreshTokenExpiresAtUtc = null;
            RefreshTokenRevokedAtUtc = revokedAtUtc;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Basarisiz giris denemesini kaydeder ve esik asildiginda hesabi kilitler.
        /// </summary>
        /// <param name="maxFailedAttempts">Kilit oncesi izin verilen deneme sayisi.</param>
        /// <param name="lockoutDuration">Kilit suresi.</param>
        public void RegisterFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration)
        {
            AccessFailedCount++;
            LastFailedLoginUtc = DateTime.UtcNow;

            if (AccessFailedCount >= maxFailedAttempts)
                LockoutEndUtc = DateTime.UtcNow.Add(lockoutDuration);

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Basarili giris sonrasi sayaci sifirlar ve son giris zamanini gunceller.
        /// </summary>
        public void RegisterSuccessfulLogin()
        {
            AccessFailedCount = 0;
            LockoutEndUtc = null;
            LastFailedLoginUtc = null;
            LastLoginDate = DateTime.UtcNow;
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
                throw new DomainException("Address.CityIdInvalid");

            if (districtId.HasValue && districtId.Value <= 0)
                throw new DomainException("Address.DistrictIdInvalid");

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

        /// <summary>
        /// Kullanicinin dil tercihini gunceller. Yalnizca desteklenen kodlar kabul edilir.
        /// Sonraki isteklerde metinler bu dile gore cozulur.
        /// </summary>
        /// <param name="languageCode">Dil kodu ("tr" veya "en").</param>
        public void SetLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                throw new DomainException("User.LanguageCodeRequired");

            var normalized = languageCode.Trim().ToLowerInvariant();

            if (!SupportedLanguages.Contains(normalized))
                throw new DomainException("User.InvalidLanguageCode");

            if (LanguageCode == normalized)
                return;

            LanguageCode = normalized;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangeEmail(Email newEmail)
        {
            if (Email == newEmail)
                throw new DomainException("User.EmailSameAsCurrent");

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
                throw new DomainException("User.PhoneSameAsCurrent");

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
                throw new DomainException("User.AppointmentNotOwned");

            _appointments.Add(appointment);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddReview(Review review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            if (review.UserId != Id)
                throw new DomainException("User.ReviewNotOwned");

            _reviews.Add(review);
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddFavorite(Favorite favorite)
        {
            if (favorite == null)
                throw new ArgumentNullException(nameof(favorite));

            if (favorite.UserId != Id)
                throw new DomainException("User.FavoriteNotOwned");

            if (_favorites.Any(f => f.BusinessId == favorite.BusinessId && !f.IsDeleted))
                throw new DomainException("Favorite.AlreadyExists");

            _favorites.Add(favorite);
            UpdatedAt = DateTime.UtcNow;
        }

        public void RemoveFavorite(Guid businessId)
        {
            var favorite = _favorites.FirstOrDefault(f => f.BusinessId == businessId && !f.IsDeleted);
            if (favorite == null)
                throw new DomainException("Favorite.NotFound");

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
                throw new DomainException("User.NotificationNotOwned");

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