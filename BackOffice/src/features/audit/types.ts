/**
 * Denetim kaydi tipleri (dokuman Modul 21). Gerekce ve deger betimlemeleri
 * sunucuda sinirlanmis metinler olarak gelir; frontend maskeleme yapmaz.
 */
export interface AuditLog {
  id: string;
  actorUserId: string;

  /** Islem anindaki yonetici rol adi. */
  actorRole: string;

  /** Ilgili isletme; platform genelinde islemlerde bos. */
  businessId?: string | null;
  entityType: string;
  entityId: string;
  action: string;

  /** Onceki degerlerin kisa JSON betimlemesi. */
  oldValues?: string | null;

  /** Yeni degerlerin kisa JSON betimlemesi. */
  newValues?: string | null;

  /** Yoneticinin islem gerekcesi. */
  reason?: string | null;
  correlationId?: string | null;
  ipAddress?: string | null;

  /** Kaydin olusturulma ani (UTC). */
  createdAtUtc: string;
}
