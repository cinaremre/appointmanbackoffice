/**
 * Kullanıcı tipleri (doküman 12). Şifre/hash alanları sunucu tarafından
 * HICBIR ZAMAN dönmez; UI'da da gösterilmez.
 */

/** GET /users liste öğesi. lastLoginDate opsiyoneldir; yoksa "-" gösterilir. */
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

/** GET /users/{id} detay yanıtı: profil + üyelikler. */
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
 * Gerekceli yönetim aksiyonları (admin.user.manage):
 * lock/unlock/revoke-sessions — hepsi body {reason} zorunlu.
 */
export type UserManageAction = "lock" | "unlock" | "revoke-sessions";
