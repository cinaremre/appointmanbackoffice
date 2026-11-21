namespace AppointMan.Application.Common.Authorization
{
    /// <summary>
    /// Backoffice permission kodlari (dokuman 5.3). Roller sabit kod
    /// kontrollerine bagli degildir: her policy bu matristen uretilir.
    /// </summary>
    public static class AdminPermissions
    {
        // ===== Permission kodlari =====
        public const string BusinessRead = "business.read";
        public const string BusinessCreate = "business.create";
        public const string BusinessUpdate = "business.update";
        public const string BusinessSuspend = "business.suspend";

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
        /// Rol -> permission eslemesi. SuperAdmin bilincli olarak matriste
        /// tutulmaz: policy cozumleyici SuperAdmin'i her zaman gecerli sayar.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RolePermissions =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["PlatformAdmin"] = new[]
                {
                    BusinessRead, BusinessCreate, BusinessUpdate, BusinessSuspend,
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
                    // Mevcut Admin rolu geriye donuk uyumluluk icin tum okuma
                    // ve sinirli yazma permission'larini tasir.
                    BusinessRead, BusinessUpdate, BusinessSuspend,
                    CategoryRead, CategoryUpdate, ServiceRead, ServiceUpdate,
                    AppointmentRead, AppointmentCancel, UserRead, UserManage,
                    SubscriptionRead, FeatureRead, FeatureOverride, FinanceRead,
                    NotificationRead, MarketingRead, ReportRead, AuditRead,
                    OperationsRead, OperationsManage
                }
            };

        /// <summary>
        /// Verilen rolun belirtilen permission'a sahip olup olmadigini cozer.
        /// SuperAdmin her permission'a sahiptir.
        /// </summary>
        /// <param name="role">JWT rol adi (case-insensitive).</param>
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
