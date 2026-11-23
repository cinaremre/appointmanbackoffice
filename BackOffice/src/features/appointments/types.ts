/**
 * Randevu tipleri (doküman 13). Durum kuralları SUNUCUDA uygulanır
 * (doküman 13.3); frontend durum mantigi üretmez.
 */

/** API'nin kabul ettigi randevu durumları. */
export const APPOINTMENT_STATUSES = [
  "Pending",
  "Confirmed",
  "Completed",
  "Cancelled",
  "NoShow",
] as const;

export type AppointmentStatus = (typeof APPOINTMENT_STATUSES)[number];

/**
 * API enum'ları sayısal serileştirir; durum etiketi burada çözülür.
 * Islemlerin geçerliliği yine SUNUCUDA belirlenir (doküman 13.3).
 */
export function appointmentStatusLabel(raw: number | string | null | undefined): string {
  if (raw === null || raw === undefined || raw === "") return "-";
  const index = Number(raw);
  if (!Number.isNaN(index) && index >= 0 && index < APPOINTMENT_STATUSES.length) {
    return APPOINTMENT_STATUSES[index];
  }
  return (APPOINTMENT_STATUSES as readonly string[]).includes(String(raw)) ? String(raw) : "-";
}

/** GET /appointments liste öğesi. */
export interface AppointmentListItem {
  id: string;
  businessId: string;
  userId: string;
  employeeId: string;
  serviceId: string;
  appointmentDate: string;
  status: AppointmentStatus;
  notes?: string | null;
}

/** GET /appointments/{id} içindeki geçmiş kaydı. */
export interface AppointmentHistoryEntry {
  action: string;
  changedAt: string;
  note?: string | null;
  actorId?: string | null;
}

/** GET /appointments/{id} detay: liste alanları + history. */
export interface AppointmentDetail extends AppointmentListItem {
  history: AppointmentHistoryEntry[];
}

/** GET /appointments/conflicts öğesi. */
export interface AppointmentConflict {
  appointmentId: string;
  employeeId: string;
  appointmentDate: string;
  businessId: string;
}

/** GET /appointments sorgu parametreleri. */
export interface AppointmentListQuery {
  businessId?: string;
  employeeId?: string;
  status?: AppointmentStatus;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}
