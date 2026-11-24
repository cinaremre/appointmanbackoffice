namespace Rezervise.Application.Common.Authorization
{
    /// <summary>
    /// Backoffice permission kodları (doküman 5.3). Roller sabit kod
    /// kontrollerine bağlı değildir: her policy bu matristen üretilir.
    /// </summary>
    public static class AdminPermissions
    {
        // ===== Permission kodları =====
        public const string BusinessRead = "business.read";
        public const string BusinessCreate = "business.create";
        public const string BusinessUpdate = "business.update";
        public const string BusinessSuspend = "business.suspend";

        /// <summary>Backoffice işletme onayı: onay bekleyen işletmeyi yayına alır.</summary>
        public const string BusinessApprove = "business.approve";

        public const string CategoryRead = "category.read";
        public const string CategoryCreate = "category.create";
        public const string CategoryUpdate = "category.update";
        public const string CategoryArchive = "category.archive";

        public const string ServiceRead = "service.read";
        public const string ServiceUpdate = "service.update";
        public const string ServiceBulkUpdate = "service.bulk_update";

        public const string AppointmentRead = "appointment.read";
        public const string AppointmentCancel = "appointment.cancel";
        public const string AppointmentOverride = "appointment.override";

        public const string UserRead = "admin.user.read";
        public const string UserManage = "admin.user.manage";

        public const string SubscriptionRead = "subscription.read";
        public const string SubscriptionUpdate = "subscription.update";

        public const string FeatureRead = "feature.read";
        public const string FeatureOverride = "feature.override";

        public const string FinanceRead = "finance.read";

        public const string NotificationRead = "notification.read";
        public const string NotificationRetry = "notification.retry";

        public const string MarketingRead = "marketing.read";
        public const string MarketingManage = "marketing.manage";

        public const string StockRead = "stock.read";
        public const string PackageRead = "package.read";

        public const string ReportRead = "report.read";
        public const string AuditRead = "audit.read";

        public const string OperationsRead = "operations.read";
        public const string OperationsManage = "operations.manage";

        /// <summary>
        /// Rol -> permission eşlemesi. SuperAdmin bilinçli olarak matriste
        /// tutulmaz: policy çözümleyici SuperAdmin'i her zaman geçerli sayar.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["PlatformAdmin"] = new[]
                {
                    BusinessRead, BusinessCreate, BusinessUpdate, BusinessSuspend, BusinessApprove,
                    CategoryRead, CategoryCreate, CategoryUpdate, CategoryArchive,
                    ServiceRead, ServiceUpdate, ServiceBulkUpdate,
                    AppointmentRead, AppointmentCancel, AppointmentOverride,
                    UserRead, UserManage,
                    SubscriptionRead, FeatureRead, FinanceRead, StockRead, PackageRead,
                    NotificationRead, MarketingRead, ReportRead, AuditRead,
                    OperationsRead, OperationsManage
                },
                ["FinanceAdmin"] = new[]
                {
                    SubscriptionRead, SubscriptionUpdate, FeatureRead, FinanceRead,
                    ReportRead, BusinessRead, AuditRead, OperationsRead
                },
                ["SupportAgent"] = new[]
                {
                    BusinessRead, AppointmentRead, AppointmentCancel,
                    UserRead, NotificationRead, OperationsRead
                },
                ["MarketingAdmin"] = new[]
                {
                    MarketingRead, MarketingManage, ReportRead, BusinessRead, AuditRead
                },
                ["ContentManager"] = new[]
                {
                    CategoryRead, CategoryCreate, CategoryUpdate, CategoryArchive,
                    ServiceRead, ServiceUpdate, ServiceBulkUpdate
                },
                ["Auditor"] = new[]
                {
                    AuditRead, BusinessRead, AppointmentRead, UserRead,
                    FinanceRead, OperationsRead, ReportRead, SubscriptionRead,
                    NotificationRead, MarketingRead, StockRead, PackageRead
                },
                ["ReadOnlyAnalyst"] = new[]
                {
                    ReportRead, BusinessRead, AppointmentRead, SubscriptionRead
                },
                ["Admin"] = new[]
                {
                    // Mevcut Admin rolü geriye dönük uyumluluk için tüm okuma
                    // ve sınırlı yazma permission'larını taşır.
                    BusinessRead, BusinessUpdate, BusinessSuspend, BusinessApprove,
                    CategoryRead, CategoryUpdate, ServiceRead, ServiceUpdate,
                    AppointmentRead, AppointmentCancel, UserRead, UserManage,
                    SubscriptionRead, FeatureRead, FeatureOverride, FinanceRead,
                    NotificationRead, MarketingRead, ReportRead, AuditRead,
                    OperationsRead, OperationsManage
                }
            };

        /// <summary>
        /// Verilen rolün belirtilen permission'a sahip olup olmadığını çözer.
        /// SuperAdmin her permission'a sahiptir.
        /// </summary>
        /// <param name="role">JWT rol adı (case-insensitive).</param>
        /// <param name="permission">Permission kodu.</param>
        /// <returns>Rol izinliyse true.</returns>
        public static bool HasPermission(string role, string permission)
        {
            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(permission))
                return false;

            if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return true;

            return RolePermissions.TryGetValue(role, out var permissions)
                && permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }
    }
}
