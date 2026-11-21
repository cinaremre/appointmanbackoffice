# Push Bildirim (Firebase Cloud Messaging) Kurulumu

Mimari: müşteri API'si domain event'lerinden push kuyruğuna yazar;
`PushBackgroundService` kuyruğu boşaltır — **FCM** (Firebase Cloud Messaging)
HTTP v1 ile gönderir. SMS/e-posta hatlarıyla aynı desen: sağlayıcı
yapılandırılmamışsa **log modu** devrede (uygulama patlamaz, bildirimler log'a
yazılır). Android'de push fiilen yalnızca FCM ile mümkündür; iOS'ta APNs'i
FCM tek entegrasyonla sarmalar — iki platform tek API'den yönetilir.

## Kod tarafı hazır olanlar

- `DeviceToken` tablosu (kullanıcı başına token, unique, aktif/pasif) — migration `AddDeviceTokens`
- `POST api/Notification/user/{userId}/device-token` — cihaz kaydı (kendi hesabı; IDOR korumalı, idempotent)
- `DELETE api/Notification/user/{userId}/device-token` — çıkışta token pasifleştirme
- Randevu olaylarında (oluşturuldu/onaylandı/iptal/ertelendi/tamamlandı/çalışan değişti) aktif cihazlara otomatik push
- `PushBackgroundService`: 3 deneme → dead-letter log; FCM `UNREGISTERED` dönerse token otomatik pasifleştirilir
- `LogPushNotificationSender`: yapılandırma yoksa log'a yazar — akış bugün test edilebilir durumda

## Sizin yapmanız gerekenler (üyelik + anahtarlar)

1. **Google hesabı açın** → https://console.firebase.google.com →
   **"Add project"** ile `rezervise` (veya istediğiniz ad) projesi oluşturun.
   FCM push gönderimi **Spark (ücretsiz) planda** çalışır — kart bilgisi gerekmez.

2. **Mobil uygulamaları projeye ekleyin** (aynı konsolda):
   - Project Overview → **Android uygulaması ekle** → paket adı (ör. `com.rezervise.app`) → `google-services.json` indirin → Android projenize koyun.
   - **iOS uygulaması ekle** → Bundle ID → `GoogleService-Info.plist` indirin → Xcode projesine ekleyin.

3. **iOS için APNs anahtarı** (Android için gerekmez):
   - Apple Developer hesabınızda bir **APNs Auth Key (.p8)** oluşturun (Keys sekmesi, "Apple Push Notifications Authentication Key").
   - Firebase Console → Project Settings → **Cloud Messaging → Apple App Configurations → APNs Authentication Key** yükleyin.
   - Bu adım iOS cihazlara bildirim gitmesi için şarttır; Android'de hiç gerekmez.

4. **Server (backend) anahtarı** — tek gereken bu:
   - Firebase Console → ⚙️ **Project Settings → Service accounts** →
     (varsayılan hesap seçili) → **"Generate new private key"** →
     `rezervise-firebase-adminsdk-xxx.json` dosyası iner.
   - Bu dosya **gizlidir**; repoya/sunucu görünebilir yerlere koymayın.

5. **Sunucuda yapılandırma** — `AppointMan.Api`'nin eriştiği ortam değişkenleri (ya da appsettings `Push` bölümü):
   ```
   Push__Enabled=true
   Push__ProjectId=<Firebase Project ID (konsolda Settings > General'da yazar)>
   Push__ServiceAccountPath=/opt/appointman/secrets/fcm-service-account.json
   ```
   .json dosyasını sunucuda örn. `/opt/appointman/secrets/` altına koyup
   `chmod 600` verin. (Docker ile müşteri API'si çalışıyorsa compose `environment:`
   bölümüne aynı değişkenleri + volume ile dosyayı ekleyin.)

6. **Mobil uygulama tarafı** (React Native / Flutter):
   - Uygulama açılışında FCM token alın (Android'de `FirebaseMessagingService`,
     iOS'ta APNs token otomatik FCM'e bağlanır).
   - Token'ı login sonrası API'ye gönderin:
     ```
     POST /api/Notification/user/{kendiUserId}/device-token
     { "token": "<fcm-token>", "platform": 0 }   // 0=Android, 1=iOS
     ```
   - Çıkışta: `DELETE /api/Notification/user/{kendiUserId}/device-token`.

## Test ortamında bugün çalışan davranış

`Push__Enabled=false` (varsayılan): cihaz kayıt uçları ve kuyruk tamamen
çalışır; "gönderim" log moduna yazılır (`Push bildirimi (log modu)...`).
Yani tüm altyapıyı FCM anahtarı olmadan uçtan uca test edebilirsiniz.

## Maliyet notu

FCM ücretsizdir; ücret yok. Firebase projesinin Analytics/Crashlytics gibi
ek hizmetleri de Spark planda isteğe bağlıdır.
