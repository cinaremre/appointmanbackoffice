# Rezervise iş akışları

Bu dosya randevu yaşam döngüsünün tek kaynaklarını ve otomatik yan etkileri özetler.

## Kimlik doğrulama

Kayıt sonrası iki doğrulama kanalı vardır; **ikisinin birden onaylanması gerekmez**:

- `POST api/Auth/verify-phone` — telefon OTP. Test ortamında SMS sağlayıcısı olmadığı için
  `Otp:FixedCode = 123456` geçerlidir (`appsettings.Development.json`).
- `GET/POST api/Auth/verify-email` — e-posta bağlantısı. SMTP yapılandırılmadığında
  doğrulama e-postası log'a yazılır; bu akış girişi **engellemez**.

`LoginCommandHandler` yalnızca **ikisi de** doğrulanmamışsa `Auth.PhoneNotVerified` döndürür.
OTP doğrulandığı anda e-posta doğrulanmamış olsa bile giriş başarılıdır. Uçtan uca akış
`Rezervise.Tests/Application/RegisterVerifyLoginFlowTests` ile doğrulanır.

## Durum makinesi

Geçerli geçişler `Rezervise.Domain.Entities.Appointment.IsValidStatusTransition` içinde tanımlıdır.
Zaman kuralları `Appointment.CanTransitionTo` ile aynı matris üzerinden uygulanır.

| Kaynak | Hedef | Koşul |
|---|---|---|
| Pending | Confirmed | Randevu gelecekte |
| Pending | Cancelled | Randevu gelecekte |
| Pending | NoShow | Randevu geçmişte |
| Pending | Pending | Yeniden planlama |
| Confirmed | Completed | Randevu geçmişte |
| Confirmed | Cancelled | Randevu gelecekte |
| Confirmed | NoShow | Randevu geçmişte |
| Confirmed | Pending | Yeniden planlama |

`Completed`, `Cancelled` ve `NoShow` terminal durumlardır. `SetStatusForSeed` yalnızca seed
için doğrulamayı atlar; üretim akışında kullanılmaz.

## Zaman kuralları

`Rezervise.Domain.Common.WorkflowPolicy` sabitleri hem handler'lar hem arka plan servisi tarafından kullanılır:

- `ReminderLeadTime`: 24 saat
- `MinimumActionLeadTime`: 2 saat (müşteri iptal/erteleme sınırı)
- `AutoCompleteGracePeriod`: 1 saat
- `SweepInterval`: 5 dakika

`AppointmentWorkflowBackgroundService` her taramada:

1. Onaylı ve 24 saat içindeki randevular için hatırlatma kuyruğa yazılır,
2. Süresi 1 saat geçmiş onaylı randevular `Completed` yapılır,
3. Süresi 1 saat geçmiş bekleyen randevular `NoShow` yapılır.

## İzin onayı

`EmployeeTimeOff` için `ApproveTimeOff` / `RejectTimeOff` komutları ve
`POST api/Employee/{timeOffId}/approve` / `.../reject` uçları vardır.
Onay yalnızca işletme sahibi veya yönetici tarafından yapılabilir. Onay öncesinde
çalışanın aktif randevularıyla izin aralığı çakışıyorsa işlem `409 Conflict` ile reddedilir.

## Domain event yan etkileri

`DomainEventBase` her olaya benzersiz `Id` verir. `EventSideEffectDeduplicator` aynı olayın
yeniden yayınlanmasında yan etki üretmesini engeller. `AppointmentEventSideEffectHandler`:

- olayı `IWorkflowAuditSink` üzerinden yapılandırılmış log/GELF kanalına yazar,
- müşteriye `IEmailQueue` üzerinden durum bildirimi kuyruğa alır.

Kuyruğu `EmailBackgroundService` boşaltır. Geçici hatalarda 3 deneme yapılır; tüm denemeler
başarısız olursa mesaj `EmailDeadLetter` kaydıyla yapılandırılmış loga yazılır ve kuyruk
boşalmaya devam eder. SMTP yapılandırılmadığında log taşıması kullanılır. Seed sırasında
yan etkiler geçici olarak kapatılır.

## Puan tutarlılığı

`IBusinessRatingUpdater.RecalculateAsync`, yorum güncelleme/silme/gizleme/gösterme sonrasında
`TotalReviews` ve `Rating` değerlerini görünür yorumlardan tek SQL UPDATE ile yeniden hesaplar.
Yorum değişikliği ile puan güncellemesi aynı transaction içinde yapılır.

## Yetki

İşletmeye ait mutation uçlarında `OwnerId`/`UserId` gövdeden alınmaz; `ICurrentUserService`
JWT'den çözer. Handler'lar `CallerAuthorization.CanActForBusiness` ile işletme sahibi veya
yönetici (Admin/SuperAdmin) doğrulaması yapar.

## Ticari modüller (AddCommercialModules migration'ı)

Dokümandaki modüllerin ilk sürüm çekirdekleri aynı desenle uygulanır:

- **Kasa/Satış (Modül 2)**: `Sale`/`SaleLine`/`Payment` (ters kayıt iadesi), `CashSession`
  (Beklenen Nakit = açılış + nakit tahsilat + CashIn − CashOut − nakit iade; kart hariç),
  açık oturum DB unique index'i ile tek. Satış ile tahsilat ayrı kaydedilir.
- **Stok (Modül 3)**: bakiye `StockMovement` defterinden hesaplanır; hareket değiştirilemez,
  aynı (ReferenceType, ReferenceId) iki kez işlenemez, negatif stok reddedilir;
  `ServiceMaterial` hizmet reçetesi tanımlar.
- **Prim (Modül 4)**: `CommissionRule` pencere kontrollü; tahakkuk tahsilat tabanlı,
  Accrued→Paid/Reversed tek yönlü. Ödeme dönemleri `CommissionPayout` ile ayrı.
- **Seans paketi (Modül 5)**: hak `PackageUsage` hareketlerinden hesaplanır; Reserve→Consume
  (randevu Completed olunca) / Release (iptal); aynı randevuya çift rezervasyon reddedilir.
- **Bekleme listesi (Modül 10)**: yarı otomatik mod; teklif 10 dk geçerli (`WaitlistPolicy`),
  kabulda slot sunucuda yeniden doğrulanır, ilk kabul diğer Pending teklifleri Superseded yapar.
- **Rezervasyon sayfası/QR (Modül 1/8)**: `BusinessPublicPage` (sistem genelinde benzersiz slug,
  anonim erişim), `TrackingLink`/`TrackingEvent` anonim olay defteri.
- **Kampanya (Modül 9)**: `MarketingCampaign`/`CampaignSpend`/`AppointmentAttribution`
  (randevu başına tek atıf), ROAS raporu satış entegrasyonu sonrasına açıklamalı.
- **Veri aktarımı (Modül 7)**: CSV (paket bağımlılıksız `CsvParser`), header satırı sayım dışı,
  telefon anahtarıyla mükerrer atlama, tamamlanan iş tekrar çalıştırılamaz.
- **Entitlement (Modül 14)**: öncelik aktif override > abonelik planı > temel varsayılan
  (`FeatureCodes.BasicDefaults`) > yok. Kota `FeatureUsage` upsert'i ile korunur.
  Platform yönetici uçları Admin/SuperAdmin + `IWorkflowAuditSink` denetim kaydı.
- **İzin (Modül 16)**: pazarlama Withdrawn kanalı tamamen kapatır (`CommunicationSuppression`),
  Transactional varsayılan açık, Marketing varsayılan kapalı; `ConsentHistory` append-only.
- **Geri çağırma (Modül 13)**: adaylık `IntervalDays − LeadDays` eşiği, gelecek randevu ve
  cooldown elemeleri; medyan ziyaret aralığı `CustomerServiceHistory`'de.
- **Dashboard (Modül 15.1)**: günün randevu/tamamlanan/iptal/no-show kartları ve basit doluluk.

Sağlayıcı gerektiren entegrasyonlar (WhatsApp Cloud/Meta/Google) bu sürümde soyutlama
+ log sağlayıcı düzeyindedir; canlı API bağlantıları ayrı faz.

## Backoffice Portal API

`Rezervise.BackOffice.Api` ayrı host'tur; mevcut Application/Infrastructure katmanlarını
yeniden kullanır (domain kuralları tek kaynak — iptal/abonelik/override komutları mevcut
handler'ları sarar).

- **Kimlik**: ayrı JWT audience (`Rezervise-Backoffice`) — müşteri token'ları geçersiz.
- **Yetki**: permission bazlı policy'ler; rol→permission matrisi
  `AdminPermissions.RolePermissions` içinde (SuperAdmin her izne sahip).
  Roller: SuperAdmin, PlatformAdmin, FinanceAdmin, SupportAgent, MarketingAdmin,
  ContentManager, Auditor, ReadOnlyAnalyst (EnumUserRole 7–13).
- **Denetim**: kritik işlemler `AdminAuditLogs` tablosuna (append-only) yazılır;
  correlation + IP `IAdminAuditContext` üzerinden taşınır.
- **Standart**: `/api/v1/admin` base; `{data, pagination, meta}` envelope; ProblemDetails
  hataları; sayfalama max 200 (varsayılan 50); rate limit + CORS allowlist ayrı.
- **Modüller**: businesses, categories (döngü korumalı taşıma), services + bulk
  operations (dryRun → onay), users (lock/unlock/revoke-sessions), appointments
  (domain iptali sarmalar), subscriptions/features (mevcut handler'lar sarılır),
  finance/stock/packages (salt okunur), notifications (retry kabul kaydı), marketing
  (kampanya/waitlist/reengagement), operations (import işleri, health), audit logs.

Doğrulama: `dotnet build Rezervise.slnx` — BackOffice.Api solution'a dahildir.

## Doğrulama

```bash
dotnet build Rezervise.slnx
dotnet test Rezervise.Tests/Rezervise.Tests.csproj --no-build
```

Entegrasyon testleri SQL Server LocalDB üzerinde geçici bir veritabanı oluşturur ve
`EnsureCreated` ile şema kurar. Uygulamanın migration/seed akışına dokunmaz.
