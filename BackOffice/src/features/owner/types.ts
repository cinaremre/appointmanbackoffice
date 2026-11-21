/**
 * Owner portal tipleri ve etiket cozumleyicileri.
 *
 * Kapsam: JWT kimliginden turetilen isletme. Alan adlari backend DTO'larinin
 * System.Text.Json camelCase karsiliklaridir:
 * - Dashboard kartlari: BusinessDashboardDto (Reporting/GetBusinessDashboard).
 * - Gunluk satis ozeti: DailySalesSummaryDto (Sales/GetDailySalesSummary).
 * Durum alanlari sayisal serilesir (0 tabanli enum); etiketler bu dosyada
 * cozumlenir, islem gecerliligi her zaman SUNUCUDA belirlenir.
 */

// === Isletme ===

/** GET /owner/businesses liste ogesi. */
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

/** GET /owner/businesses/{id} detayi: liste alanlari + ozet sayilari. */
export interface OwnedBusinessDetail extends OwnedBusinessListItem {
  activeServiceCount: number;
  employeeCount: number;
  todaysAppointmentCount: number;
}

// === Dashboard (BusinessDashboardDto + DailySalesSummaryDto) ===

/** Gunluk randevu kartlari; alan adlari backend DTO'suyla birebir. */
export interface OwnerDashboardDto {
  businessId: string;
  date: string;
  /** Bugunku beklemede/onayli randevu sayisi. */
  bugunkiRandevular: number;
  /** Bugun tamamlanan randevu sayisi. */
  tamamlanan: number;
  /** Bugun iptal edilen randevu sayisi. */
  iptaller: number;
  /** Bugun gelmeyen (no-show) randevu sayisi. */
  noShow: number;
  /** 0-1 arasi doluluk orani (iki haneye yuvarlanir). */
  dolulukOrani: number;
}

/** Gunluk satis ozeti; tum tutarlar net degerdir (iadeler dusulur). */
export interface DailySalesSummaryDto {
  date: string;
  businessId: string;
  /** Tamamlanan satis sayisi. */
  salesCount: number;
  /** Gunun satis toplami (GrandTotal). */
  totalSales: number;
  /** Net nakit tahsilat (iadeler dusulmus). */
  cashCollected: number;
  /** Net kart tahsilat (iadeler dusulmus). */
  cardCollected: number;
  /** Gunun gider toplami. */
  totalExpenses: number;
  /** Gunun satislarinda henuz odenmemis bakiye. */
  outstandingReceivables: number;
  /** Isletmede acik kasa oturumu var mi? */
  isCashOpen: boolean;
}

/** GET /owner/businesses/{id}/dashboard yanitinin govdesi. */
export interface OwnerDashboardResponse {
  dashboard: OwnerDashboardDto;
  summary: DailySalesSummaryDto;
}

// === Randevular ===

/** GET /owner/businesses/{id}/appointments ogesi; status sayisaldir. */
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

/** GET /owner/businesses/{id}/services ogesi. */
export interface OwnerServiceListItem {
  id: string;
  name: string;
  price: number;
  currency: string;
  durationMinutes: number;
  isActive: boolean;
}

// === Calisanlar ve izinler ===

/** GET /owner/businesses/{id}/employees ogesi. */
export interface OwnerEmployeeListItem {
  id: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  isActive: boolean;
}

/** GET /owner/businesses/{id}/time-offs ogesi; status sayisaldir. */
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
 * GET /owner/businesses/{id}/finance/sales ogesi. Owner satis ucu liste-only
 * sade DTO dondurur; toplam kolonu gerekiyorsa sonradan eklenir.
 */
export interface OwnerSaleListItem {
  id: string;
  soldAt: string;
  grandTotal: number;
  currency: string;
  /** EnumSaleStatus: 0=Pending, 1=Completed, 2=Cancelled, 3=Refunded. */
  status: number;
}

/** GET /owner/businesses/{id}/finance/payments ogesi. */
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

/** GET /owner/businesses/{id}/finance/cash-sessions ogesi. */
export interface OwnerCashSessionListItem {
  id: string;
  openedAt: string;
  openingCash: number;
  closedAt?: string | null;
  expectedCash?: number | null;
  countedCash?: number | null;
  /** Sayilan - beklenen nakit farki; acik oturumda null. */
  difference?: number | null;
  /** EnumCashSessionStatus: 0=Open, 1=Closed. */
  status: number;
}

// === Stok ve paketler ===

/** GET /owner/businesses/{id}/stock/products ogesi. */
export interface OwnerProductListItem {
  id: string;
  sku: string;
  name: string;
  salePrice: number;
  minimumStock: number;
  isActive: boolean;
  /** Guncel stok bakiyesi; minimum altina dustugunde vurgulanir. */
  balance: number;
}

/** GET /owner/businesses/{id}/stock/movements ogesi. */
export interface OwnerStockMovementListItem {
  id: string;
  productId: string;
  /** Hareket turu (sayisal); bilinmeyen deger ham gosterilir. */
  movementType: number;
  quantity: number;
  unitCost: number;
  referenceType?: string | null;
  occurredAt: string;
}

/** GET /owner/businesses/{id}/packages ogesi: musterinin aldig paket hakki. */
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
  /** Kalan kullanilabilir hak (purchased - consumed - reserved). */
  remaining: number;
}

// === Yorumlar ===

/** GET /owner/businesses/{id}/reviews ogesi. */
export interface OwnerReviewListItem {
  id: string;
  customerId: string;
  rating: number;
  comment?: string | null;
  isHidden: boolean;
  createdAt: string;
}

// === Durum etiketleri (sayisal enum karsiliklari) ===

/** EnumAppointmentStatus: 0=Pending..4=NoShow (admin etiketleriyle uyumlu). */
export const APPOINTMENT_STATUS_LABELS: Record<number, string> = {
  0: "Pending",
  1: "Confirmed",
  2: "Completed",
  3: "Cancelled",
  4: "NoShow",
};

/** Randevu durumu filtre secenekleri (URL query degerleri string'dir). */
export const APPOINTMENT_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "0", label: "Pending" },
  { value: "1", label: "Confirmed" },
  { value: "2", label: "Completed" },
  { value: "3", label: "Cancelled" },
  { value: "4", label: "NoShow" },
];

/** Izin durumu: 0=Beklemede, 1=Onaylandi, 2=Reddedildi. */
export const TIME_OFF_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Onaylandi",
  2: "Reddedildi",
};

export const TIME_OFF_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "", label: "Tumu" },
  { value: "0", label: "Beklemede" },
  { value: "1", label: "Onaylandi" },
  { value: "2", label: "Reddedildi" },
];

/** EnumSaleStatus: 0=Pending, 1=Completed, 2=Cancelled, 3=Refunded. */
export const SALE_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Tamamlandi",
  2: "Iptal",
  3: "Iade",
};

/** EnumPaymentMethod: 0=Cash..5=PackageCredit (kisa owner etiketleri). */
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
  0: "Acik",
  1: "Kapali",
};

export const CASH_SESSION_STATUS_OPTIONS: ReadonlyArray<{ value: string; label: string }> = [
  { value: "0", label: "Acik" },
  { value: "1", label: "Kapali" },
];

/**
 * Stok hareket turu (sayisal). Sunucu enum karsiligi sabitlenmediginden
 * bilinmeyen degerler ham gosterilir.
 */
export const STOCK_MOVEMENT_TYPE_LABELS: Record<number, string> = {
  0: "Giris",
  1: "Cikis",
  2: "Duzeltme",
};

/**
 * Musteri paket durumu (sayisal). Sunucu enum karsiligi sabitlenmediginden
 * bilinmeyen degerler ham gosterilir.
 */
export const CUSTOMER_PACKAGE_STATUS_LABELS: Record<number, string> = {
  0: "Aktif",
  1: "Tukendi",
  2: "Suresi Doldu",
};

/** Sayisal enum → etiket; bilinmeyen deger ham gosterilir, eksikte "-". */
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

/** Izin durumu etiketi (0-2). */
export function timeOffStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, TIME_OFF_STATUS_LABELS);
}

/** Satis durumu etiketi (0-3). */
export function saleStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, SALE_STATUS_LABELS);
}

/** Odeme yontemi etiketi (0-5). */
export function paymentMethodLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, PAYMENT_METHOD_LABELS);
}

/** Kasa durumu etiketi (0-1). */
export function cashSessionStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, CASH_SESSION_STATUS_LABELS);
}

/** Stok hareket turu etiketi. */
export function movementTypeLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, STOCK_MOVEMENT_TYPE_LABELS);
}

/** Musteri paket durumu etiketi. */
export function packageStatusLabel(raw: number | string | null | undefined): string {
  return ownerLabelFor(raw, CUSTOMER_PACKAGE_STATUS_LABELS);
}

/** Aktiflik rozeti metni. */
export function activeText(isActive: boolean): string {
  return isActive ? "Aktif" : "Pasif";
}

// === Bicimlendirme yardimcilari (owner kopyasi; finance modulunden import edilmez) ===

/** Para bicimi: tr-TR binlik ayirici + TRY icin "TL" kisa adi. */
export function formatMoney(value: number, currency = "TRY"): string {
  return `${value.toLocaleString("tr-TR")} ${currency === "TRY" ? "TL" : currency}`;
}

/** Isaretli para bicimi: pozitif "+" oneki tasir, negatifte "-" zaten vardir. */
export function formatSignedMoney(value: number, currency = "TRY"): string {
  return `${value > 0 ? "+" : ""}${formatMoney(value, currency)}`;
}

/** Kisa tarih+saat bicimi; gecersiz degerde ham metni dondurur. */
export function formatDateTime(iso?: string | null): string {
  if (!iso) return "-";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
}

/** Sade tarih bicimi; gecersiz degerde ham metni dondurur. */
export function formatDateOnly(iso?: string | null): string {
  if (!iso) return "-";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleDateString("tr-TR");
}

/** Bugunun UTC ISO tarih kismi (date input baslangic degeri). */
export function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

/** GUID kisa gosterim; tam deger title ozelliginde tasir. */
export function shortId(value: string): string {
  return value.slice(0, 8);
}
