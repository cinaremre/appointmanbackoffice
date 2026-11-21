/**
 * Finans modulu tipleri (dokuman Modul 16-17).
 *
 * Controller okuma notlari:
 * - GET /finance/sales  -> OkEnvelope ile AdminSalesListDto doner:
 *   { items, page, pageSize, totalCount, totalPages, hasMore, totalGrandTotal }.
 *   Liste + toplam tek nesne icinde geldiginden client tarafinda getItem +
 *   manual cozumleme kullanilir (getList degil).
 * - GET /finance/payments | cash-sessions | expenses | commissions -> OkList
 *   ile doner: { data: [...], pagination: {...} }; getList ile okunur.
 * - Odemelerde iadeler IsRefund bayragiyla ayri listelenir (true = iadeler).
 */

/** GET /finance/sales tek satiri. */
export interface SaleRow {
  id: string;
  businessId: string;
  customerId?: string | null;
  soldAt: string;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  grandTotal: number;
  currency: string;
  status: number | string;
}

/** Sayfalanmis satis listesi + filtre kapsaminin GrandTotal toplami. */
export interface AdminSalesListDto {
  items: SaleRow[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages?: number;
  hasMore?: boolean;
  /** Filtreye uyan TUM satislarin toplam tutari (yalnizca sayfa degil). */
  totalGrandTotal?: number | null;
}

/** GET /finance/payments tek satiri; iade kayitlari RefersToPaymentId ile baglanir. */
export interface PaymentRow {
  id: string;
  businessId: string;
  saleId: string;
  cashSessionId?: string | null;
  amount: number;
  currency: string;
  paymentMethod: number | string;
  paidAt: string;
  status: number | string;
  isRefund: boolean;
  refersToPaymentId?: string | null;
}

/** GET /finance/cash-sessions tek satiri; Difference kapanista hesaplanir. */
export interface CashSessionRow {
  id: string;
  businessId: string;
  cashRegisterId: string;
  openedBy: string;
  openedAt: string;
  openingCash: number;
  closedBy?: string | null;
  closedAt?: string | null;
  expectedCash?: number | null;
  countedCash?: number | null;
  /** Sayilan - beklenen nakit farki; acik oturumda null. */
  difference?: number | null;
  status: number | string;
}

/** GET /finance/expenses tek satiri. */
export interface ExpenseRow {
  id: string;
  businessId: string;
  categoryId?: string | null;
  amount: number;
  currency: string;
  expenseDate: string;
  description?: string | null;
  paymentMethod: number | string;
  status: number | string;
}

/** GET /finance/commissions tek satiri. */
export interface CommissionRow {
  id: string;
  businessId: string;
  employeeId: string;
  saleLineId?: string | null;
  ruleId: string;
  baseAmount: number;
  commissionAmount: number;
  status: number | string;
  calculatedAt: string;
}

/** EnumSaleStatus: 0=Pending, 1=Completed, 2=Cancelled, 3=Refunded. */
export const SALE_STATUS_LABELS: Record<number, string> = {
  0: "Beklemede",
  1: "Tamamlandi",
  2: "Iptal",
  3: "Iade",
};

/** EnumPaymentMethod: 0=Cash, 1=BankCard, 2=Transfer, 3=Online, 4=GiftCard, 5=PackageCredit. */
export const PAYMENT_METHOD_LABELS: Record<number, string> = {
  0: "Nakit",
  1: "Banka Karti",
  2: "Havale/EFT",
  3: "Online",
  4: "Hediye Ceki",
  5: "Paket Kredisi",
};

/** EnumCashSessionStatus: 0=Open, 1=Closed. */
export const CASH_SESSION_STATUS_LABELS: Record<number, string> = {
  0: "Acik",
  1: "Kapali",
};

/** EnumCommissionAccrualStatus: 0=Accrued, 1=Paid, 2=Reversed. */
export const COMMISSION_STATUS_LABELS: Record<number, string> = {
  0: "Tahakkuk",
  1: "Odenmis",
  2: "Ters Cevrildi",
};

/** Enum sayi veya string gelebilir; toleransli etiket dondurur. */
export function labelFor(raw: unknown, labels: Record<number, string>): string {
  if (typeof raw === "number") return labels[raw] ?? String(raw);
  if (typeof raw === "string") {
    const numeric = Number(raw);
    return labels[numeric] ?? raw;
  }
  return "-";
}

/** Para bicimi: tr-TR binlik ayirici + TRY icin "TL" kisa adi. */
export function formatMoney(value: number, currency = "TRY"): string {
  return `${value.toLocaleString("tr-TR")} ${currency === "TRY" ? "TL" : currency}`;
}

/** Isaretli para bicimi: pozitif "+" oneki tasir, negatifte "-" zaten vardir. */
export function formatSignedMoney(value: number, currency = "TRY"): string {
  return `${value > 0 ? "+" : ""}${formatMoney(value, currency)}`;
}

/** Kisa tarih+saat bicimi; gecersiz degerde ham metni dondurur. */
export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
}

/** Basit GUID dogrulamasi: filtre girisleri icin. */
export function isGuid(value: string): boolean {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
    value.trim(),
  );
}
