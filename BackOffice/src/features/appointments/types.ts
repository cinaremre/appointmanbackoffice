/**
 * Randevu tipleri (dokuman 13). Durum kurallari SUNUCUDA uygulanir
 * (dokuman 13.3); frontend durum mantigi uretmez.
 */

/** API'nin kabul ettigi randevu durumlari. */
export const APPOINTMENT_STATUSES = [
  "Pending",
  "Confirmed",
  "Completed",
  "Cancelled",
  "NoShow",
] as const;

export type AppointmentStatus = (typeof APPOINTMENT_STATUSES)[number];

/**
 * API enum'lari sayisal serilestirir; durum etiketi burada cozulur.
 * Islemlerin gecerliligi yine SUNUCUDA belirlenir (dokuman 13.3).
 */
export function appointmentStatusLabel(raw: number | string | null | undefined): string {
  if (raw === null || raw === undefined || raw === "") return "-";
  const index = Number(raw);
  if (!Number.isNaN(index) && index >= 0 && index < APPOINTMENT_STATUSES.length) {
    return APPOINTMENT_STATUSES[index];
  }
  return (APPOINTMENT_STATUSES as readonly string[]).includes(String(raw)) ? String(raw) : "-";
}

/** GET /appointments liste ogesi. */
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

/** GET /appointments/{id} icindeki gecmis kaydi. */
export interface AppointmentHistoryEntry {
  action: string;
  changedAt: string;
  note?: string | null;
  actorId?: string | null;
}

/** GET /appointments/{id} detay: liste alanlari + history. */
export interface AppointmentDetail extends AppointmentListItem {
  history: AppointmentHistoryEntry[];
}

/** GET /appointments/conflicts ogesi. */
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
