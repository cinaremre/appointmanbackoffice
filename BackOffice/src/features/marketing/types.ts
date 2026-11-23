/**
 * Pazarlama modülü tipleri (doküman Modül 15). Backend enum'ları sayı olarak
 * serileştirilir; sayısal değerler Domain enum'larıyla birebir eşleşir.
 */

/** EnumCampaignChannel: kampanyanın yurutuldugu kanal. */
export type CampaignChannel = 0 | 1 | 2 | 3 | 4 | 5;

export const CAMPAIGN_CHANNEL_LABELS: Record<number, string> = {
  0: "Instagram",
  1: "Meta Ads",
  2: "Google Ads",
  3: "Organik",
  4: "Direkt",
  5: "Diger",
};

/** Kampanya listesi öğesi (AdminCampaignDto). */
export interface CampaignListItem {
  id: string;
  businessId: string;
  name: string;
  channel: number;
  externalCampaignId?: string | null;
  startAt?: string | null;
  endAt?: string | null;
  isActive: boolean;

  /** Kampanyaya yapılan toplam harcama tutarı. */
  totalSpend: number;

  /** Kampanyaya atfedilen randevu sayısı. */
  attributedAppointments: number;
}

/** Kampanya performans özeti (AdminCampaignPerformanceDto). */
export interface CampaignPerformance {
  campaignId: string;
  businessId: string;
  campaignName: string;
  totalSpend: number;
  attributedAppointments: number;
  attributedCompleted: number;
  roasDesc: string;
}

/** EnumReengagementStatus: geri çağırma denemesinin durumu. */
export type ReengagementStatus = 0 | 1 | 2 | 3 | 4;

export const REENGAGEMENT_STATUS_LABELS: Record<number, string> = {
  0: "Uygun",
  1: "Bildirim Gonderildi",
  2: "Dönüştü",
  3: "Engellendi",
  4: "Bekleme Suresinde",
};

/** Geri çağırma denemesi (AdminReengagementAttemptDto). */
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

/** EnumRecoveredSlotStatus: iptal edilen randevudan açığa çıkan slotun durumu. */
export type RecoveredSlotStatus = 0 | 1 | 2;

export const RECOVERED_SLOT_STATUS_LABELS: Record<number, string> = {
  0: "Açık",
  1: "Yeniden Dolduruldu",
  2: "Kaybedildi",
};

/** Açığa çıkan slot (AdminRecoveredSlotDto). */
export interface RecoveredSlot {
  id: string;
  businessId: string;
  cancelledAppointmentId: string;
  replacementAppointmentId?: string | null;
  releasedAt: string;
  rebookedAt?: string | null;
  status: number;
}

/** Kurtarılan kapasite raporu (AdminRecoveredSlotReportDto). */
export interface RecoveredSlotReport {
  items: RecoveredSlot[];
  totalRebooked: number;
}

/** Bekleme listesi özet sayıları (AdminWaitlistOverviewDto). */
export interface WaitlistOverview {
  businessId?: string | null;
  activeEntries: number;
  pendingOffers: number;
  acceptedOffers: number;
  expiredOffers: number;
  rebookedSlots: number;
  lostSlots: number;
}
