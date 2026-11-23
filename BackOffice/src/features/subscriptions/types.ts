/**
 * Abonelik ve özellik yönetimi tipleri (doküman Modül 14-15).
 *
 * Backend uçları AdminSubscriptionController üzerinden camelCase JSON döner.
 * Enum'lar varsayılan System.TextJson davranışıyla SAYISAL serileştirilir
 * (0=Monthly, 1=Yearly gibi); okuma tarafında hem sayı hem string kabul
 * eden toleranslı çözümleyiciler kullanılır, yazma tarafında ise sayısal
 * değer gönderilir.
 */

export type BillingPeriod = "Monthly" | "Yearly";

/** Plana dahil tek özellik satırı. includedQuota null = sınırsız. */
export interface AdminPlanFeatureDto {
  featureCode: string;
  isEnabled: boolean;
  includedQuota: number | null;
}

/** AdminSubscriptionController GET /subscription-plans tek satırı. */
export interface SubscriptionPlanDto {
  id: string;
  name: string;
  price: number;
  currency: string;
  billingPeriod: BillingPeriod;
  isActive: boolean;
  features: AdminPlanFeatureDto[];
}

/** GET /features tek satırı: platform özellik katalogu. */
export interface FeatureDefinitionDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
}

/** GET /businesses/{id}/features tek override kaydı. */
export interface FeatureOverrideDto {
  id: string;
  featureId: string;
  featureCode: string;
  isEnabled: boolean;
  /** Null ise plan kotası geçerli kalır. */
  quotaOverride?: number | null;
  validFrom: string;
  /** UTC geçerlilik sonu; null = belirsiz süre. */
  validUntil?: string | null;
  reason?: string | null;
  grantedBy: string;
}

/** İşletme aboneliğine dahil plan özelliği. */
export interface BusinessPlanFeatureDto {
  featureCode: string;
  isEnabled: boolean;
  includedQuota: number | null;
}

/** GET /businesses/{id}/subscription; aktif abonelik yoksa data null gelir. */
export interface BusinessSubscriptionDto {
  subscriptionId: string;
  planId: string;
  planName: string;
  startAt: string;
  endAt?: string | null;
  status: number | string;
  planFeatures: BusinessPlanFeatureDto[];
}

export const BILLING_PERIOD_LABELS: Record<BillingPeriod, string> = {
  Monthly: "Aylık",
  Yearly: "Yıllık",
};

/** POST gövdesinde enum sayısal gönderilir (0=Monthly, 1=Yearly). */
export const BILLING_PERIOD_VALUES: Record<BillingPeriod, number> = {
  Monthly: 0,
  Yearly: 1,
};

/** Enum sayı veya string gelebilir; toleranslı çözer, tanınmayanda Monthly. */
export function toBillingPeriod(raw: unknown): BillingPeriod {
  if (raw === 1 || raw === "1" || raw === "Yearly") return "Yearly";
  return "Monthly";
}

/** EnumSubscriptionStatus: 0=Active, 1=Expired, 2=Cancelled. */
export const SUBSCRIPTION_STATUS_LABELS: Record<number, string> = {
  0: "Aktif",
  1: "Süre Doldu",
  2: "İptal",
};

export function subscriptionStatusLabel(raw: unknown): string {
  if (typeof raw === "number") return SUBSCRIPTION_STATUS_LABELS[raw] ?? String(raw);
  if (typeof raw === "string" && raw !== "") return raw;
  return "-";
}

/** Basit GUID doğrulaması: işletme kimliği girişi için. */
export function isGuid(value: string): boolean {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
    value.trim(),
  );
}

/** Fiyat/tutar etiketi: tr-TR biçimi + TRY için "TL" kısa adı. */
export function formatPrice(value: number, currency: string): string {
  return `${value.toLocaleString("tr-TR")} ${currency === "TRY" ? "TL" : currency}`;
}

/** Kota etiketi; null/undefined = sınırsız (plan kotası geçerli). */
export function quotaLabel(quota: number | null | undefined): string {
  if (quota === null || quota === undefined) return "Sınırsız";
  return String(quota);
}
