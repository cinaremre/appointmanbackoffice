using System.Security.Cryptography;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.User;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Entities
{
    public class User : BaseEntity
    {
        /// <summary>Desteklenen dil kodları.</summary>
        private static readonly string[] SupportedLanguages = { "tr", "en" };

        /// <summary>Varsayılan dil kodu.</summary>
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

        // Güvenlik: refresh token yalnızca hash'lenmiş olarak saklanır; ham değer DB'de tutulmaz.
        public string? RefreshTokenHash { get; private set; }
        public string? PreviousRefreshTokenHash { get; private set; }
        public DateTime? RefreshTokenExpiresAtUtc { get; private set; }
        public DateTime? RefreshTokenCreatedAtUtc { get; private set; }
        public DateTime? RefreshTokenRevokedAtUtc { get; private set; }

        // Güvenlik: e-posta doğrulama token'i da yalnızca hash'lenmiş olarak saklanır.
        // Kullanıcı basına tek aktif token tutulur; yeni token eskisini geçersiz kılar.
        public string? EmailVerificationTokenHash { get; private set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; private set; }

        /// <summary>
        /// Doğrulama e-postasının en son istendiği zaman (UTC).
        /// Yeniden gönderme şıklığını sınırlamak için kullanılır.
        /// </summary>
        public DateTime? EmailVerificationRequestedAt { get; private set; }

        // Güvenlik: brute force koruması için başarısız giriş sayacı ve kilit bitiş zamanı.
        public int AccessFailedCount { get; private set; }
        public DateTime? LockoutEndUtc { get; private set; }

        /// <summary>Hesabın su anda kilitli olup olmadığını bildirir.</summary>
        public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;

        /// <summary>Son başarılı giriş zamanı (UTC).</summary>
        public DateTime? LastFailedLoginUtc { get; private set; }

        // Konum: kullanıcının kayıtlı öldüğü il / ilçe.
        // Arama kriteri boş geldiğinde varsayılan konum olarak kullanılır.
        public int? CityId { get; private set; }
        public int? DistrictId { get; private set; }

        // Dil tercihi: "tr" veya "en". Kullanıcıya dönen metinlerin kültürü
        // bu değer üzerinden belirlenir.
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
            // Token tek kullanımlıktır: doğrulama başarılı olunca temizlenir.
            ClearEmailVerificationToken();
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserEmailVerifiedEvent(Id, Email, $"{FirstName} {LastName}"));
        }

        /// <summary>
        /// Yeni bir e-posta doğrulama token'i kaydeder ve varsa önceki token'i geçersiz kılar.
        /// </summary>
        /// <param name="tokenHash">Ham token'in SHA-256 hash'i; ham değer asla saklanmaz.</param>
        /// <param name="expiresAt">Token'in mutlak bitiş zamanı (UTC).</param>
        /// <param name="requestedAt">E-postanın istendiği zaman (UTC); yeniden gönderme sınırı için.</param>
        /// <exception cref="DomainException">Token hash'i boş öldüğünda fırlatılır.</exception>
        public void IssueEmailVerificationToken(string tokenHash, DateTime expiresAt, DateTime requestedAt)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new DomainException("User.EmailVerificationTokenRequired");

            EmailVerificationTokenHash = tokenHash;
            EmailVerificationTokenExpiresAt = expiresAt;
            EmailVerificationRequestedAt = requestedAt;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Aktif e-posta doğrulama token'ini temizler. Token yoksa da çağrılabilir.
        /// </summary>
        public void ClearEmailVerificationToken()
        {
            EmailVerificationTokenHash = null;
            EmailVerificationTokenExpiresAt = null;
            EmailVerificationRequestedAt = null;
        }

        // Güvenlik: telefon (OTP) doğrulama kodu da yalnızca hash'lenmiş olarak saklanır.
        // Kullanıcı basına tek aktif kod tutulur; yeni kod eskisini geçersiz kılar.
        public string? PhoneVerificationCodeHash { get; private set; }
        public DateTime? PhoneVerificationCodeExpiresAt { get; private set; }
        public DateTime? PhoneVerificationRequestedAt { get; private set; }
        public int PhoneVerificationAttemptCount { get; private set; }

        /// <summary>
        /// Yeni telefon doğrulama kodunun hash'ini kaydeder ve deneme sayacını sıfırlar.
        /// </summary>
        /// <param name="codeHash">Kodun SHA-256 hash'i; ham kod asla saklanmaz.</param>
        /// <param name="expiresAt">Kodun mutlak bitiş zamanı (UTC).</param>
        /// <param name="requestedAt">Kodun üretildiği zaman (UTC).</param>
        /// <exception cref="DomainException">Hash boş öldüğünda fırlatılır.</exception>
        public void IssuePhoneVerificationCode(string codeHash, DateTime expiresAt, DateTime requestedAt)
        {
            if (string.IsNullOrWhiteSpace(codeHash))
                throw new DomainException("User.PhoneVerificationCodeRequired");

            PhoneVerificationCodeHash = codeHash;
            PhoneVerificationCodeExpiresAt = expiresAt;
            PhoneVerificationRequestedAt = requestedAt;
            PhoneVerificationAttemptCount = 0;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Aktif telefon doğrulama kodunu temizler.</summary>
        public void ClearPhoneVerificationCode()
        {
            PhoneVerificationCodeHash = null;
            PhoneVerificationCodeExpiresAt = null;
            PhoneVerificationAttemptCount = 0;
        }

        /// <summary>Başarısız doğrulama denemesi sayacını arttırır.</summary>
        public void IncrementPhoneVerificationAttempt()
        {
            PhoneVerificationAttemptCount++;
            UpdatedAt = DateTime.UtcNow;
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
        /// Yeni refresh token hash'ini kaydeder ve mevcut hash'i tekrar kullanım
        /// (reuse) tespiti için önceki slota taşır.
        /// </summary>
        /// <param name="refreshTokenHash">Yeni refresh token'in SHA-256 hash'i.</param>
        /// <param name="expiresAtUtc">Yeni token'in mutlak bitiş zamanı (UTC).</param>
        /// <param name="issuedAtUtc">Token'in üretildiği zaman (UTC).</param>
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
        /// Refresh token durumunu iptal eder. Çıkış, parola değişimi ve
        /// token tekrar kullanımı tespitinde çağrılır.
        /// </summary>
        /// <param name="revokedAtUtc">İptal zamanı (UTC).</param>
        public void RevokeRefreshToken(DateTime revokedAtUtc)
        {
            RefreshTokenHash = null;
            PreviousRefreshTokenHash = null;
            RefreshTokenExpiresAtUtc = null;
            RefreshTokenRevokedAtUtc = revokedAtUtc;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Başarısız giriş denemesini kaydeder ve eşik asıldığında hesabı kilitler.
        /// </summary>
        /// <param name="maxFailedAttempts">Kilit öncesi izin verilen deneme sayısı.</param>
        /// <param name="lockoutDuration">Kilit süresi.</param>
        public void RegisterFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration)
        {
            AccessFailedCount++;
            LastFailedLoginUtc = DateTime.UtcNow;

            if (AccessFailedCount >= maxFailedAttempts)
                LockoutEndUtc = DateTime.UtcNow.Add(lockoutDuration);

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Başarılı giriş sonrası sayacı sıfırlar ve son giriş zamanını günceller.
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
        /// Kullanıcının kayıtlı konumunu (il / ilçe) günceller.
        /// Arama sırasında kriter boş geldiğinde bu bilgi kullanılır.
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
        /// Kullanıcının dil tercihini günceller. Yalnızca desteklenen kodlar kabul edilir.
        /// Sonraki isteklerde metinler bu dile göre çözülür.
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

        /// <summary>Placeholder first name applied when an account is erased.</summary>
        private const string AnonymizedFirstName = "Anonim";

        /// <summary>Placeholder last name applied when an account is erased.</summary>
        private const string AnonymizedLastName = "Kullanıcı";

        /// <summary>
        /// Erases every personal data field on the account (KVKK anonymization) and marks
        /// the record as soft deleted. The row is retained because appointment, review,
        /// favourite, notification and blacklist rows reference it through Restrict FKs.
        /// </summary>
        /// <param name="utcNow">Moment of erasure, stored in <c>UpdatedAt</c> and <c>DeletedAt</c>.</param>
        /// <returns>The anonymized e-mail address that replaced the original one.</returns>
        public string AnonymizeAndDelete(DateTime utcNow)
        {
            // 1. Doğrudan tanımlayıcılar
            FirstName = AnonymizedFirstName;
            LastName = AnonymizedLastName;

            // 2. İletişim tanımlayıcıları: her kayıt için benzersiz yer tutucular.
            //    Sabit bir değer kullanılsaydı ikinci silmede unique index ihlali olurdu.
            Email = BuildAnonymizedEmail(Id);
            PhoneNumber = BuildAnonymizedPhoneNumber(Id);

            // 3. Kimlik bilgisi: BCrypt bu değeri asla doğrulayamaz.
            //    Kolon zorunlu ve 500 karakter sınırlı öldüğü için yer tutucu şart.
            PasswordHash = BuildUnusablePasswordHash();

            // 4. Oturum yeteneği ve doğrulama token'lari temizlenir.
            RevokeRefreshToken(utcNow);
            ClearEmailVerificationToken();

            // 5. Davranışsal ve dolaylı tanımlayıcılar silinir.
            IsEmailVerified = false;
            IsPhoneVerified = false;
            DateOfBirth = null;
            ProfilePictureUrl = null;
            LastLoginDate = null;
            CityId = null;
            DistrictId = null;
            AccessFailedCount = 0;
            LockoutEndUtc = null;

            // 6. Dil tercihi, rol ve hesap durumu kişisel veri değildir; korunur.

            // 7. Soft delete: satır referans bütünlüğü için kalır.
            IsDeleted = true;
            DeletedAt = utcNow;
            UpdatedAt = utcNow;

            return Email;
        }

        /// <summary>
        /// Reports whether the user still owns at least one non deleted business.
        /// Business ownership must be transferred before the account can be erased.
        /// </summary>
        /// <remarks>
        /// The caller must eager load the OwnedBusinesses navigation; otherwise the
        /// collection is empty and this method silently returns false.
        /// </remarks>
        /// <returns><c>true</c> when ownership transfer is still required.</returns>
        public bool OwnsAnyBusiness()
        {
            return _ownedBusinesses.Any(b => !b.IsDeleted);
        }

        /// <summary>
        /// Builds the per-record unique placeholder e-mail for a deleted account.
        /// </summary>
        /// <param name="userId">Identifier of the account being erased.</param>
        /// <returns>Deterministic address of the form deleted-{32 hex}@anonim.local.</returns>
        private static string BuildAnonymizedEmail(Guid userId)
        {
            // 53 karakter: 8 on ek + 32 hex + 13 son ek; Email kolonu 255 karaktere kadar.
            return $"deleted-{userId:N}@anonim.local";
        }

        /// <summary>
        /// Builds the per-record unique placeholder phone number for a deleted account.
        /// The value is produced through the PhoneNumber value object so its format
        /// validation and normalization always run.
        /// </summary>
        /// <param name="userId">Identifier of the account being erased.</param>
        /// <returns>A normalized +905XXXXXXXXX PhoneNumber value object.</returns>
        private static PhoneNumber BuildAnonymizedPhoneNumber(Guid userId)
        {
            // Guid baytlarından 9 haneli sayısal gövde üretilir; +905 öneki ile
            // geçerli bir mobil numara formatı elde edilir (toplam 13 karakter).
            var bytes = userId.ToByteArray();
            var seed = BitConverter.ToUInt64(bytes, 0);
            var body = (int)(seed % 1_000_000_000);

            return new PhoneNumber($"+905{body:D9}");
        }

        /// <summary>
        /// Creates a password hash that can never be matched by BCrypt verification.
        /// </summary>
        /// <returns>Opaque placeholder string within the 500 character column limit.</returns>
        private static string BuildUnusablePasswordHash()
        {
            // BCrypt bu değeri asla doğrulayamaz: geçerli bir hash formatı değil.
            var noise = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            return $"!DELETED!{noise}";
        }
    }
}