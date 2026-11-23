/**
 * Owner portal tipleri ve etiket çözümleyicileri.
 *
 * Kapsam: JWT kimliğinden türetilen işletme. Alan adları backend DTO'larının
 * System.Text.Json camelCase karşılıklarıdır:
 * - Dashboard kartları: BusinessDashboardDto (Reporting/GetBusinessDashboard).
 * - Günlük satış özeti: DailySalesSummaryDto (Sales/GetDailySalesSummary).
 * Durum alanları sayısal serileşir (0 tabanlı enum); etiketler bu dosyada
 * çözümlenir, işlem geçerliliği her zaman SUNUCUDA belirlenir.
 */

// === İşletme ===

/** GET /owner/businesses liste öğesi. */
export interface OwnedBusinessListItem {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  rating: number;
  totalReviews: number;
  cityId: string;
  createdAt: string;
}

/** GET /owner/businesses/{id} detayı: liste alanları + özet sayıları. */
export interface OwnedBusinessDetail extends OwnedBusinessListItem {
  activeServiceCount: number;
  employeeCount: number;
  todaysAppointmentCount: number;
}

// === Dashboard (BusinessDashboardDto + DailySalesSummaryDto) ===

/** Günlük randevu kartları; alan adları backend DTO'suyla birebir. */
export interface OwnerDashboardDto {
  businessId: string;
  date: string;
  /** Bugünkü beklemede/onaylı randevu sayısı. */
  bugunkiRandevular: number;
  /** Bugün tamamlanan randevu sayısı. */
  tamamlanan: number;
  /** Bugün iptal edilen randevu sayısı. */
  iptaller: number;
  /** Bugün gelmeyen (no-show) randevu sayısı. */
  noShow: number;
  /** 0-1 arası doluluk oranı (iki haneye yuvarlanir). */
  dolulukOrani: number;
}

/** Günlük satış özeti; tüm tutarlar net değerdir (iadeler düşülür). */
export interface DailySalesSummaryDto {
  date: string;
  businessId: string;
  /** Tamamlanan satış sayısı. */
  salesCount: number;
  /** Günün satış toplamı (GrandTotal). */
  totalSales: number;
  /** Net nakit tahsilat (iadeler düşülmüş). */
  cashCollected: number;
  /** Net kart tahsilat (iadeler düşülmüş). */
  cardCollected: number;
  /** Günün gider toplamı. */
  totalExpenses: number;
  /** Günün satışlarında henüz ödenmemiş bakiye. */
  outstandingReceivables: number;
  /** İşletmede açık kasa oturumu var mı? */
  isCashOpen: boolean;
}

/** GET /owner/businesses/{id}/dashboard yanıtının gövdesi. */
export interface OwnerDashboardResponse {
  dashboard: OwnerDashboardDto;
  summary: DailySalesSummaryDto;
}

// === Randevular ===

/** GET /owner/businesses/{id}/appointments öğesi; status sayısaldır. */
export interface OwnerAppointmentListItem {
  id: string;
  userId: string;
  employeeId: string;
  serviceId: string;
  appointmentDate: string;
  /** 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled, 4=NoShow. */
  status: number;
  notes?: string | null;
}

// === Hizmetler ===

/** GET /owner/businesses/{id}/services öğesi. */
export interface OwnerServiceListItem {
  id: string;
  name: string;
  price: number;
  currency: string;
  durationMinutes: number;
  isActive: boolean;
}

// === Çalışanlar ve izinler ===

/** GET /owner/businesses/{id}/employees öğesi. */
export interface OwnerEmployeeListItem {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  isActive: boolean;
}

/** GET /owner/businesses/{id}/time-offs öğesi; status sayısaldır. */
export interface OwnerTimeOffListItem {
  id: string;
  employeeId: string;
  startDate: string;
  endDate: string;
  /** 0=Pending, 1=Approved, 2=Rejected. */
  status: number;
  reason?: string | null;
}

// === Finans ===

/**
 * GET /owner/businesses/{id}/finance/sales öğesi. Owner satış ucu liste-only
 * sade DTO döndürür; toplam kolonu gerekiyorsa sonradan eklenir.
 */
export interface OwnerSaleListItem {
  id: string;
  soldAt: string;
  grandTotal: number;
  currency: string;
  /** EnumSaleStatus: 0=Pending, 1=Completed, 2=Cancelled, 3=Refunded. */
  status: number;
}

/** GET /owner/businesses/{id}/finance/payments öğesi. */
export interface OwnerPaymentListItem {
  id: string;
  saleId: string;
  amount: number;
  currency: string;
  /** EnumPaymentMethod: 0=Cash..5=PackageCredit. */
  paymentMethod: number;
  paidAt: string;
  isRefund: boolean;
}

/** GET /owner/businesses/{id}/finance/cash-sessions öğesi. */
export interface OwnerCashSessionListItem {
  id: string;
  openedAt: string;
  openingCash: number;
  closedAt?: string | null;
  expectedCash?: number | null;
  countedCash?: number | null;
  /** Sayılan - beklenen nakit farkı; açık oturumda null. */
  difference?: number | null;
  /** EnumCashSessionStatus: 0=Open, 1=Closed. */
  status: number;
}

// === Stok ve paketler ===

/** GET /owner/businesses/{id}/stock/products öğesi. */
export interface OwnerProductListItem {
  id: string;
  sku: string;
  name: string;
  salePrice: number;
  minimumStock: number;
  isActive: boolean;
  /** Guncel stok bakiyesi; minimum altına düştüğünde vurgulanır. */
  balance: number;
}

/** GET /owner/businesses/{id}/stock/movements öğesi. */
export interface OwnerStockMovementListItem {
  id: string;
  productId: string;
  /** Hareket türü (sayısal); bilinmeyen değer ham gösterilir. */
  movementType: number;
  quantity: number;
  unitCost: number;
  referenceType?: string | null;
  occurredAt: string;
}

/** GET /owner/businesses/{id}/packages öğesi: müşterinin aldığı paket hakkı. */
export interface OwnerCustomerPackageListItem {
  id: string;
  customerId: string;
  packageId: string;
  purchasedAt: string;
  expiresAt?: string | null;
  status: number;
  purchased: number;
  consumed: number;
  reserved: number;
  /** Kalan kullanılabilir hak (purchased - consumed - reserved). */
  remaining: number;
}

// === Yorumlar ===

/** GET /owner/businesses/{id}/reviews öğesi. */
export interface OwnerReviewListItem {
  id: string;
  customerId: string;
  rating: number;
  comment?: string | null;
  isHidden: boolean;
  createdAt: string;
}

// === Durum etiketleri (sayısal enum karşılıkları) ===

/** EnumAppointmentStatus: 0=Pending..4=NoShow (admin etiketleriyle uyumlu). */
export const APPOINTMENT_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Confirmed",
  2: "Completed",
  3: "Cancelled",
  4: "NoShow",
};

/** Randevu durumu filtre seçenekleri (URL query değerleri string'dir). */
export const APPOINTMENT_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "0", label: "Pending" },
  { value: "1", label: "Confirmed" },
  { value: "2", label: "Completed" },
  { value: "3", label: "Cancelled" },
  { value: "4", label: "NoShow" },
];

/** İzin durumu: 0=Beklemede, 1=Onaylandı, 2=Reddedildi. */
export const TIME_OFF_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Onaylandı",
  2: "Reddedildi",
};

export const TIME_OFF_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "", label: "Tümü" },
  { value: "0", label: "Beklemede" },
  { value: "1", label: "Onaylandı" },
  { value: "2", label: "Reddedildi" },
];

/** EnumSaleStatus: 0=Pending, 1=Completed, 2=Cancelled, 3=Refunded. */
export const SALE_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Tamamlandı",
  2: "İptal",
  3: "İade",
};

/** EnumPaymentMethod: 0=Cash..5=PackageCredit (kısa owner etiketleri). */
export const PAYMENT_METHOD_LABELS: Record<number, string> = {
  0: "Nakit",
  1: "Kart",
  2: "Havale",
  3: "Online",
  4: "Hediye",
  5: "Paket",
};

/** EnumCashSessionStatus: 0=Open, 1=Closed. */
export const CASH_SESSION_STATUS_LABELS: Record<number, string> = {
  0: "Açık",
  1: "Kapalı",
};

export const CASH_SESSION_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "0", label: "Açık" },
  { value: "1", label: "Kapalı" },
];

/**
 * Stok hareket türü (sayısal). Sunucu enum karşılığı sabitlenmediginden
 * bilinmeyen değerler ham gösterilir.
 */
export const STOCK_MOVEMENT_TYPE_LABELS: Record<number, string> = {
  0: "Giriş",
  1: "Çıkış",
  2: "Duzeltme",
};

/**
 * Müşteri paket durumu (sayısal). Sunucu enum karşılığı sabitlenmediginden
 * bilinmeyen değerler ham gösterilir.
 */
export const CUSTOMER_PACKAGE_STATUS_LABELS: Record<number, string> = {
  0: "Aktif",
  1: "Tukendi",
  2: "Süresi Doldu",
};

/** Sayısal enum → etiket; bilinmeyen değer ham gösterilir, eksikte "-". */
export function ownerLabelFor(
  raw: number | string | null | undefined,
  labels: Record<number, string>,
): string {
  if (raw === null || raw === undefined) return "-";
  const numeric = typeof raw === "number" ? raw : Number(raw);
  if (!Number.isNaN(numeric)) return labels[numeric] ?? String(raw);
  return String(raw);
}

/** Randevu durumu etiketi (0-4). */
export function appointmentStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, APPOINTMENT_STATUS_LABELS);
}

/** İzin durumu etiketi (0-2). */
export function timeOffStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, TIME_OFF_STATUS_LABELS);
}

/** Satış durumu etiketi (0-3). */
export function saleStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, SALE_STATUS_LABELS);
}

/** Ödeme yöntemi etiketi (0-5). */
export function paymentMethodLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, PAYMENT_METHOD_LABELS);
}

/** Kasa durumu etiketi (0-1). */
export function cashSessionStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, CASH_SESSION_STATUS_LABELS);
}

/** Stok hareket türü etiketi. */
export function movementTypeLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, STOCK_MOVEMENT_TYPE_LABELS);
}

/** Müşteri paket durumu etiketi. */
export function packageStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, CUSTOMER_PACKAGE_STATUS_LABELS);
}

/** Aktiflik rozeti metni. */
export function activeText(isActive: boolean): string {
  return isActive ? "Aktif" : "Pasif";
}

// === Biçimlendirme yardımcıları (owner kopyası; finance modülünden import edilmez) ===

/** Para biçimi: tr-TR binlik ayirici + TRY için "TL" kısa adı. */
export function formatMoney(value: number, currency = "TRY"): string {
  return `${value.toLocaleString("tr-TR")} ${currency === "TRY" ? "TL" : currency}`;
}

/** Isaretli para biçimi: pozitif "+" öneki taşır, negatifte "-" zaten vardir. */
export function formatSignedMoney(value: number, currency = "TRY"): string {
  return `${value > 0 ? "+" : ""}${formatMoney(value, currency)}`;
}

/** Kısa tarih+saat biçimi; geçersiz degerde ham metni döndürür. */
export function formatDateTime(iso?: string | null): string {
  if (!iso) return "-";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
}

/** Sade tarih biçimi; geçersiz degerde ham metni döndürür. */
export function formatDateOnly(iso?: string | null): string {
  if (!iso) return "-";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString("tr-TR");
}

/** Bugunun UTC ISO tarih kısmi (date input başlangıç degeri). */
export function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

/** GUID kısa gösterim; tam değer title ozelliginde taşır. */
export function shortId(value: string): string {
  return value.slice(0, 8);
}
