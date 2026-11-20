# AppointMan iş akışları

Bu dosya randevu yaşam döngüsünün tek kaynaklarını ve otomatik yan etkileri özetler.

## Kimlik doğrulama

Kayıt sonrası iki doğrulama kanalı vardır; **ikisinin birden onaylanması gerekmez**:

- `POST api/Auth/verify-phone` — telefon OTP. Test ortamında SMS sağlayıcısı olmadığı için
  `Otp:FixedCode = 123456` geçerlidir (`appsettings.Development.json`).
- `GET/POST api/Auth/verify-email` — e-posta bağlantısı. SMTP yapılandırılmadığında
  doğrulama e-postası log'a yazılır; bu akış girişi **engellemez**.

`LoginCommandHandler` yalnızca **ikisi de** doğrulanmamışsa `Auth.PhoneNotVerified` döndürür.
OTP doğrulandığı anda e-posta doğrulanmamış olsa bile giriş başarılıdır. Uçtan uca akış
`AppointMan.Tests/Application/RegisterVerifyLoginFlowTests` ile doğrulanır.

## Durum makinesi

Geçerli geçişler `AppointMan.Domain.Entities.Appointment.IsValidStatusTransition` içinde tanımlıdır.
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

`AppointMan.Domain.Common.WorkflowPolicy` sabitleri hem handler'lar hem arka plan servisi tarafından kullanılır:

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

## Doğrulama

```bash
dotnet build AppointMan.slnx
dotnet test AppointMan.Tests/AppointMan.Tests.csproj --no-build
```

Entegrasyon testleri SQL Server LocalDB üzerinde geçici bir veritabanı oluşturur ve
`EnsureCreated` ile şema kurar. Uygulamanın migration/seed akışına dokunmaz.
