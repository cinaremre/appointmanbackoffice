using Rezervise.Domain.Entities;
using Rezervise.Domain.Events;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rezervise.Infrastructure.Persistence.Context
{
    public class AppDbContext : DbContext
    {
        private readonly IDomainEventDispatcher? _domainEventDispatcher;
        private bool _dispatchingDomainEvents;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IDomainEventDispatcher? domainEventDispatcher = null)
            : base(options)
        {
            _domainEventDispatcher = domainEventDispatcher;
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
        public DbSet<DeviceToken> DeviceTokens { get; set; }

        // ===== Kasa ve satis (Modul 2) =====
        public DbSet<CashRegister> CashRegisters { get; set; }
        public DbSet<CashSession> CashSessions { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleLine> SaleLines { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CashMovement> CashMovements { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        // ===== Stok ve prim (Modul 3-4) =====
        public DbSet<Product> Products { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<ServiceMaterial> ServiceMaterials { get; set; }
        public DbSet<CommissionRule> CommissionRules { get; set; }
        public DbSet<CommissionAccrual> CommissionAccruals { get; set; }
        public DbSet<CommissionPayout> CommissionPayouts { get; set; }

        // ===== Seans paketi ve bekleme listesi (Modul 5-10) =====
        public DbSet<ServicePackage> ServicePackages { get; set; }
        public DbSet<ServicePackageItem> ServicePackageItems { get; set; }
        public DbSet<CustomerPackage> CustomerPackages { get; set; }
        public DbSet<PackageUsage> PackageUsages { get; set; }
        public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
        public DbSet<WaitlistOffer> WaitlistOffers { get; set; }
        public DbSet<RecoveredSlot> RecoveredSlots { get; set; }

        // ===== Rezervasyon sayfasi, takip, kampanya ve import (Modul 1-7-9) =====
        public DbSet<BusinessPublicPage> BusinessPublicPages { get; set; }
        public DbSet<TrackingLink> TrackingLinks { get; set; }
        public DbSet<TrackingEvent> TrackingEvents { get; set; }
        public DbSet<MarketingCampaign> MarketingCampaigns { get; set; }
        public DbSet<CampaignSpend> CampaignSpends { get; set; }
        public DbSet<AppointmentAttribution> AppointmentAttributions { get; set; }
        public DbSet<ImportJob> ImportJobs { get; set; }
        public DbSet<ImportRowResult> ImportRowResults { get; set; }

        // ===== Entitlement, izin ve geri cagirma (Modul 13-14-16) =====
        public DbSet<FeatureDefinition> FeatureDefinitions { get; set; }        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<BusinessSubscription> BusinessSubscriptions { get; set; }
        public DbSet<BusinessFeatureOverride> BusinessFeatureOverrides { get; set; }
        public DbSet<FeatureUsage> FeatureUsages { get; set; }
        public DbSet<CustomerCommunicationPreference> CustomerCommunicationPreferences { get; set; }
        public DbSet<ConsentHistory> ConsentHistories { get; set; }
        public DbSet<CommunicationSuppression> CommunicationSuppressions { get; set; }
        public DbSet<BusinessReengagementRule> BusinessReengagementRules { get; set; }
        public DbSet<CustomerServiceHistory> CustomerServiceHistories { get; set; }
        public DbSet<ReengagementAttempt> ReengagementAttempts { get; set; }

        // ===== Backoffice denetim kayitlari (Modul 21) =====
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

        // ===== Backoffice toplu islemleri (Modul 10) =====
        public DbSet<BulkOperation> BulkOperations { get; set; }

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
            // Event'ler kayit oncesi snapshot'lanir: kayit sirasinda degisen
            // entity durumlari sonrasi tekrar okunursa ayni event kacirilabilir.
            var pendingEvents = SnapshotDomainEvents();

            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);

                await DispatchDomainEventsAsync(pendingEvents, cancellationToken);

                return result;
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
        /// Takip edilen aggregate'lerde biriken domain event'lerini kayit oncesinde kopyalar.
        /// </summary>
        /// <returns>Kaynak aggregate'leriyle birlikte bekleyen event'ler.</returns>
        private List<(IAggregateRoot Aggregate, IDomainEvent Event)> SnapshotDomainEvents()
        {
            if (_domainEventDispatcher is null || _dispatchingDomainEvents)
                return new List<(IAggregateRoot, IDomainEvent)>();

            return ChangeTracker.Entries<IAggregateRoot>()
                .SelectMany(entry => entry.Entity.DomainEvents.Select(
                    domainEvent => (Aggregate: entry.Entity, Event: domainEvent)))
                .ToList();
        }

        /// <summary>
        /// Kayit basariliysa event'leri yayinlar ve kaynak aggregate'lerden temizler.
        /// </summary>
        /// <param name="pendingEvents">Kayit oncesi alinan event snapshot'i.</param>
        /// <param name="cancellationToken">Iptal tokeni.</param>
        private async Task DispatchDomainEventsAsync(
            IReadOnlyCollection<(IAggregateRoot Aggregate, IDomainEvent Event)> pendingEvents,
            CancellationToken cancellationToken)
        {
            if (pendingEvents.Count == 0)
                return;

            // Bir event handler ayni context uzerinde tekrar SaveChangesAsync
            // cagirabilir; bu bayrak ayni snapshot'in iki kez yayinlanmasini engeller.
            _dispatchingDomainEvents = true;

            try
            {
                await _domainEventDispatcher!.DispatchAsync(
                    pendingEvents.Select(pending => pending.Event).ToList(),
                    cancellationToken);
            }
            catch
            {
                // Transaction icinde yayin hatasi sessizce gecilirse commit
                // kismi islenmis bir degisikligi kalici hale getirir. Transaction
                // disinda kayit zaten tamamlandigindan yanit bozulmaz.
                if (Database.CurrentTransaction is not null)
                    throw;
            }
            finally
            {
                foreach (var pending in pendingEvents)
                    pending.Aggregate.ClearDomainEvents();

                _dispatchingDomainEvents = false;
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
    }
}