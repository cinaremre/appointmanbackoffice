using AppointMan.Domain.Entities;
using AppointMan.Domain.Events;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AppointMan.Infrastructure.Persistence.Context
{
    public class AppDbContext : DbContext, IUnitOfWork
    {
        private IDbContextTransaction? _currentTransaction;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ===== DbSet'ler =====
        public DbSet<User> Users { get; set; }
        public DbSet<BlacklistEntry> BlacklistEntries { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<BusinessCategory> BusinessCategories { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Locality> Localities { get; set; }
        public DbSet<Neighborhood> Neighborhoods { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeService> EmployeeServices { get; set; }
        public DbSet<EmployeeWorkingHour> EmployeeWorkingHours { get; set; }
        public DbSet<EmployeeTimeOff> EmployeeTimeOffs { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<ServiceCategoryTranslation> ServiceCategoryTranslations { get; set; }
        public DbSet<BusinessServiceCategory> BusinessServiceCategories { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentHistory> AppointmentHistories { get; set; }
        public DbSet<WorkingHour> WorkingHours { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        // ===== Concurrency / Constraint Translation =====
        /// <summary>
        /// SaveChanges hatalarini anlamli domain istisnalarina cevirir:
        /// 2601/2627 -> DuplicateKeyException, 1205 -> ConcurrencyConflictException,
        /// DbUpdateConcurrencyException -> ConcurrencyConflictException.
        /// Boylece handler'lardaki genel catch bloklari 500 uretmeden once
        /// API katmani 409 Conflict dondurebilir.
        /// </summary>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyConflictException(
                    "Kayit baska bir islem tarafindan degistirildi. Lutfen tekrar deneyin.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Saglayiciya bagimli olmamak icin hata, ic istisnanin mesaji
                // uzerinden siniflandirilir; boylece EF Core saglayici
                // degisse de davranis korunur.
                var message = ex.InnerException?.Message ?? string.Empty;

                if (message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
                {
                    var indexName = ExtractIndexName(message);

                    throw new DuplicateKeyException(
                        ExtractEntityName(ex),
                        MapDuplicateMessage(indexName),
                        indexName,
                        ex);
                }

                if (message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConcurrencyConflictException(
                        "Islem gecici olarak kilitlendi. Lutfen tekrar deneyin.", ex);
                }

                throw;
            }
        }

        /// <summary>
        /// SQL Server hata mesajindan ihlal edilen index veya kisit adini cikarir.
        /// Mesaj formatlari: "with unique index 'IX_Name'" veya "constraint 'UQ_Name'".
        /// </summary>
        private static string? ExtractIndexName(string message)
        {
            var match = Regex.Match(
                message,
                @"(?:unique index|constraint)\s+'(?<name>[^']+)'",
                RegexOptions.IgnoreCase);

            return match.Success ? match.Groups["name"].Value : null;
        }

        /// <summary>
        /// Ihlal edilen varlik adini entry metadata'sindan okur.
        /// Bulunamazsa genel bir ad doner.
        /// </summary>
        private static string ExtractEntityName(DbUpdateException exception)
        {
            return exception.Entries.Count > 0
                ? exception.Entries[0].Metadata.ClrType.Name
                : "Kayit";
        }

        /// <summary>
        /// Ihlal edilen index adina gore kullaniciya gosterilecek Turkce
        /// mesaji secer.
        /// </summary>
        private static string MapDuplicateMessage(string? indexName)
        {
            return indexName switch
            {
                "UX_Appointments_EmployeeId_AppointmentDate_Active"
                    => "Bu saat araliginda zaten bir randevu var.",
                "UX_Reviews_AppointmentId"
                    => "Bu randevu icin zaten bir yorum yapilmis.",
                "UX_BlacklistEntries_BusinessId_UserId"
                    => "Bu kullanici zaten kara listede.",
                "UX_Favorites_UserId_BusinessId"
                    => "Bu isletme zaten favorilerinizde.",
                "UX_Users_Email"
                    => "Bu e-posta adresi zaten kayitli.",
                "UX_Users_PhoneNumber"
                    => "Bu telefon numarasi zaten kayitli.",
                _ => "Bu kayit zaten mevcut."
            };
        }

        // ===== Transaction Management =====
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is not null)
                throw new InvalidOperationException("Zaten aktif bir transaction var.");

            _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                throw new InvalidOperationException("Aktif transaction yok.");

            try
            {
                await SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction is null)
                return;

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        // ===== Repository Factory =====
        public IRepository<T> Repository<T>() where T : class, IAggregateRoot
        {
            return new GenericRepository<T>(this);
        }

        // ===== Scoped Transaction Helper =====
        /// <summary>
        /// Verilen isi belirtilen izolasyon seviyesinde tek transaction icinde
        /// calistirir, basarili olursa commit eder, hata olursa geri alir.
        /// Rezervasyon gibi aralik cakismasi riski tasiyan akislarda
        /// <see cref="IsolationLevel.Serializable"/> kullanilir.
        /// </summary>
        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>Donus degeri olmayan isler icin ayni davranis.</summary>
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            return ExecuteInTransactionAsync(async token =>
            {
                await operation(token);
                return true;
            }, isolationLevel, cancellationToken);
        }
    }
}