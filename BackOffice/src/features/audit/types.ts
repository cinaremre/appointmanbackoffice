/**
 * Denetim kaydı tipleri (doküman Modül 21). Gerekçe ve değer betimlemeleri
 * sunucuda sınırlanmış metinler olarak gelir; frontend maskeleme yapmaz.
 */
export interface AuditLog {
  id: string;
  actorUserId: string;

  /** İşlem anındaki yönetici rol adı. */
  actorRole: string;

  /** İlgili işletme; platform genelinde işlemlerde boş. */
  businessId?: string | null;
  entityType: string;
  entityId: string;
  action: string;

  /** Önceki değerlerin kısa JSON betimlemesi. */
  oldValues?: string | null;

  /** Yeni değerlerin kısa JSON betimlemesi. */
  newValues?: string | null;

  /** Yoneticinin işlem gerekçesi. */
  reason?: string | null;
  correlationId?: string | null;
  ipAddress?: string | null;

  /** Kaydın oluşturulma anı (UTC). */
  createdAtUtc: string;
}
