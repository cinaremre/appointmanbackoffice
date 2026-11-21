/**
 * Pazarlama modulu tipleri (dokuman Modul 15). Backend enum'lari sayi olarak
 * serilestirilir; sayisal degerler Domain enum'lariyla birebir eslesir.
 */

/** EnumCampaignChannel: kampanyanin yurutuldugu kanal. */
export type CampaignChannel = 0 | 1 | 2 | 3 | 4 | 5;

export const CAMPAIGN_CHANNEL_LABELS: Record<number, string> = {
  0: "Instagram",
  1: "Meta Ads",
  2: "Google Ads",
  3: "Organik",
  4: "Direkt",
  5: "Diger",
};

/** Kampanya listesi ogesi (AdminCampaignDto). */
export interface CampaignListItem {
  id: string;
  businessId: string;
  name: string;
  channel: number;
  externalCampaignId?: string | null;
  startAt?: string | null;
  endAt?: string | null;
  isActive: boolean;

  /** Kampanyaya yapilan toplam harcama tutari. */
  totalSpend: number;

  /** Kampanyaya atfedilen randevu sayisi. */
  attributedAppointments: number;
}

/** Kampanya performans ozeti (AdminCampaignPerformanceDto). */
export interface CampaignPerformance {
  campaignId: string;
  businessId: string;
  campaignName: string;
  totalSpend: number;
  attributedAppointments: number;
  attributedCompleted: number;
  roasDesc: string;
}

/** EnumReengagementStatus: geri cagirma denemesinin durumu. */
export type ReengagementStatus = 0 | 1 | 2 | 3 | 4;

export const REENGAGEMENT_STATUS_LABELS: Record<number, string> = {
  0: "Uygun",
  1: "Bildirim Gonderildi",
  2: "Donustu",
  3: "Engellendi",
  4: "Bekleme Suresinde",
};

/** Geri cagirma denemesi (AdminReengagementAttemptDto). */
export interface ReengagementAttempt {
  id: string;
  businessId: string;
  customerId: string;
  serviceId: string;
  ruleId: string;
  notificationId?: string | null;
  triggeredAt: string;
  status: number;
  convertedAppointmentId?: string | null;
}

/** EnumRecoveredSlotStatus: iptal edilen randevudan aciga cikan slotun durumu. */
export type RecoveredSlotStatus = 0 | 1 | 2;

export const RECOVERED_SLOT_STATUS_LABELS: Record<number, string> = {
  0: "Acik",
  1: "Yeniden Dolduruldu",
  2: "Kaybedildi",
};

/** Aciga cikan slot (AdminRecoveredSlotDto). */
export interface RecoveredSlot {
  id: string;
  businessId: string;
  cancelledAppointmentId: string;
  replacementAppointmentId?: string | null;
  releasedAt: string;
  rebookedAt?: string | null;
  status: number;
}

/** Kurtarilan kapasite raporu (AdminRecoveredSlotReportDto). */
export interface RecoveredSlotReport {
  items: RecoveredSlot[];
  totalRebooked: number;
}

/** Bekleme listesi ozet sayilari (AdminWaitlistOverviewDto). */
export interface WaitlistOverview {
  businessId?: string | null;
  activeEntries: number;
  pendingOffers: number;
  acceptedOffers: number;
  expiredOffers: number;
  rebookedSlots: number;
  lostSlots: number;
}
