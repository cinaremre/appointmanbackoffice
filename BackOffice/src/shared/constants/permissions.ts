/**
 * Rol -> permission eslemesinin frontend kopyasi (dokuman 5.2/5.3).
 * Bu KULLANICI DENeyimi icindir: menuler ve aksiyon butonlari gizlenir.
 * Nihai yetki kontrolu backend policy'lerinde yapilir.
 */

export const PERMISSIONS = {
  businessRead: "business.read",
  businessUpdate: "business.update",
  businessSuspend: "business.suspend",
  categoryRead: "category.read",
  categoryCreate: "category.create",
  categoryUpdate: "category.update",
  categoryArchive: "category.archive",
  serviceRead: "service.read",
  serviceUpdate: "service.update",
  serviceBulkUpdate: "service.bulk_update",
  appointmentRead: "appointment.read",
  appointmentCancel: "appointment.cancel",
  userRead: "admin.user.read",
  userManage: "admin.user.manage",
  subscriptionRead: "subscription.read",
  subscriptionUpdate: "subscription.update",
  featureRead: "feature.read",
  featureOverride: "feature.override",
  financeRead: "finance.read",
  notificationRead: "notification.read",
  notificationRetry: "notification.retry",
  marketingRead: "marketing.read",
  stockRead: "stock.read",
  packageRead: "package.read",
  reportRead: "report.read",
  auditRead: "audit.read",
  operationsRead: "operations.read",
} as const;

export type Permission = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];

const PLATFORM_ADMIN: Permission[] = [
  PERMISSIONS.businessRead, PERMISSIONS.businessUpdate, PERMISSIONS.businessSuspend,
  PERMISSIONS.categoryRead, PERMISSIONS.categoryCreate, PERMISSIONS.categoryUpdate, PERMISSIONS.categoryArchive,
  PERMISSIONS.serviceRead, PERMISSIONS.serviceUpdate, PERMISSIONS.serviceBulkUpdate,
  PERMISSIONS.appointmentRead, PERMISSIONS.appointmentCancel,
  PERMISSIONS.userRead, PERMISSIONS.userManage,
  PERMISSIONS.subscriptionRead, PERMISSIONS.featureRead, PERMISSIONS.financeRead,
  PERMISSIONS.stockRead, PERMISSIONS.packageRead, PERMISSIONS.notificationRead,
  PERMISSIONS.marketingRead, PERMISSIONS.reportRead, PERMISSIONS.auditRead,
  PERMISSIONS.operationsRead,
];

export const ROLE_PERMISSIONS: Record<string, Permission[]> = {
  SuperAdmin: Object.values(PERMISSIONS),
  PlatformAdmin: PLATFORM_ADMIN,
  FinanceAdmin: [
    PERMISSIONS.subscriptionRead, PERMISSIONS.subscriptionUpdate, PERMISSIONS.featureRead,
    PERMISSIONS.financeRead, PERMISSIONS.reportRead, PERMISSIONS.businessRead,
    PERMISSIONS.auditRead, PERMISSIONS.operationsRead,
  ],
  SupportAgent: [
    PERMISSIONS.businessRead, PERMISSIONS.appointmentRead, PERMISSIONS.appointmentCancel,
    PERMISSIONS.userRead, PERMISSIONS.notificationRead, PERMISSIONS.operationsRead,
  ],
  MarketingAdmin: [
    PERMISSIONS.marketingRead, PERMISSIONS.reportRead, PERMISSIONS.businessRead, PERMISSIONS.auditRead,
  ],
  ContentManager: [
    PERMISSIONS.categoryRead, PERMISSIONS.categoryCreate, PERMISSIONS.categoryUpdate, PERMISSIONS.categoryArchive,
    PERMISSIONS.serviceRead, PERMISSIONS.serviceUpdate, PERMISSIONS.serviceBulkUpdate,
  ],
  Auditor: [
    PERMISSIONS.auditRead, PERMISSIONS.businessRead, PERMISSIONS.appointmentRead, PERMISSIONS.userRead,
    PERMISSIONS.financeRead, PERMISSIONS.operationsRead, PERMISSIONS.reportRead, PERMISSIONS.subscriptionRead,
    PERMISSIONS.notificationRead, PERMISSIONS.marketingRead, PERMISSIONS.stockRead, PERMISSIONS.packageRead,
  ],
  ReadOnlyAnalyst: [
    PERMISSIONS.reportRead, PERMISSIONS.businessRead, PERMISSIONS.appointmentRead, PERMISSIONS.subscriptionRead,
  ],
  Admin: [
    PERMISSIONS.businessRead, PERMISSIONS.businessUpdate, PERMISSIONS.businessSuspend,
    PERMISSIONS.categoryRead, PERMISSIONS.categoryUpdate, PERMISSIONS.serviceRead, PERMISSIONS.serviceUpdate,
    PERMISSIONS.appointmentRead, PERMISSIONS.appointmentCancel, PERMISSIONS.userRead, PERMISSIONS.userManage,
    PERMISSIONS.subscriptionRead, PERMISSIONS.featureRead, PERMISSIONS.featureOverride, PERMISSIONS.financeRead,
    PERMISSIONS.notificationRead, PERMISSIONS.marketingRead, PERMISSIONS.reportRead, PERMISSIONS.auditRead,
    PERMISSIONS.operationsRead,
  ],
};

/** Rol listesinden permission'a sahip mi? SuperAdmin her izne sahiptir. */
export function hasPermission(roles: string[], permission: Permission): boolean {
  if (roles.includes("SuperAdmin")) return true;
  return roles.some((role) => ROLE_PERMISSIONS[role]?.includes(permission));
}
