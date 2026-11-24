/**
 * İŞLETME modülü tipleri (doküman 9).
 * Alanlar backend envelope ile birebir eşleşir; frontend tahmin üretmez.
 */

export type BusinessStatus = "Active" | "Suspended" | "PendingApproval";

/** GET /businesses liste öğesi. */
export interface BusinessListItem {
  id: string;
  name: string;
  status: BusinessStatus;
  cityId: string;
  ownerId: string;
  createdAt: string;
  rating: number;
  totalReviews: number;
}

/** GET /businesses/{id} tekil işletme. */
export interface BusinessDetail {
  id: string;
  name: string;
  description?: string | null;
  status: BusinessStatus;
  isApproved: boolean;
  cityId: string;
  ownerId: string;
  employeeCount: number;
  activeServiceCount: number;
  recentAppointmentCount: number;
  createdAt: string;
}

/** GET /businesses/{id}/audit denetim kaydı öğesi. */
export interface AuditEntry {
  actorUserId: string;
  actorRole: string;
  action: string;
  reason?: string | null;
  oldValues?: Record<string, unknown> | null;
  newValues?: Record<string, unknown> | null;
  createdAtUtc: string;
}

/** Liste filtreleri (doküman 22): status/search URL query'de korunur. */
export interface BusinessListFilters {
  status: string;
  search: string;
}
