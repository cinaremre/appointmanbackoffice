/**
 * ISLETME modulu tipleri (dokuman 9).
 * Alanlar backend envelope ile birebir eslesir; frontend tahmin uretmez.
 */

export type BusinessStatus = "Active" | "Suspended";

/** GET /businesses liste ogesi. */
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

/** GET /businesses/{id} tekil isletme. */
export interface BusinessDetail {
  id: string;
  name: string;
  description?: string | null;
  status: BusinessStatus;
  cityId: string;
  ownerId: string;
  employeeCount: number;
  activeServiceCount: number;
  recentAppointmentCount: number;
  createdAt: string;
}

/** GET /businesses/{id}/audit denetim kaydi ogesi. */
export interface AuditEntry {
  actorUserId: string;
  actorRole: string;
  action: string;
  reason?: string | null;
  oldValues?: Record<string, unknown> | null;
  newValues?: Record<string, unknown> | null;
  createdAtUtc: string;
}

/** Liste filtreleri (dokuman 22): status/search URL query'de korunur. */
export interface BusinessListFilters {
  status: string;
  search: string;
}
