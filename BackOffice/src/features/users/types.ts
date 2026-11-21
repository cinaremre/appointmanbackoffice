/**
 * Kullanici tipleri (dokuman 12). Sifre/hash alanlari sunucu tarafindan
 * HICBIR ZAMAN donmez; UI'da da gosterilmez.
 */

/** GET /users liste ogesi. lastLoginDate opsiyoneldir; yoksa "-" gosterilir. */
export interface UserListItem {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  role: string;
  isLockedOut: boolean;
  lastLoginDate?: string | null;
  createdAt: string;
}

export interface UserOwnedBusiness {
  id: string;
  name: string;
}

export interface UserEmployeePosition {
  employeeId: string;
  businessId: string;
}

/** GET /users/{id} detay yaniti: profil + uyelikler. */
export interface UserDetail {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  role: string;
  isLockedOut: boolean;
  lastLoginDate?: string | null;
  ownedBusinesses: UserOwnedBusiness[];
  employeePositions: UserEmployeePosition[];
  createdAt: string;
}

/** GET /users sorgu parametreleri. */
export interface UserListQuery {
  search?: string;
  email?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

/**
 * Gerekceli yonetim aksiyonlari (admin.user.manage):
 * lock/unlock/revoke-sessions — hepsi body {reason} zorunlu.
 */
export type UserManageAction = "lock" | "unlock" | "revoke-sessions";
