# AppointMan.Api — Güvenlik Katmanı Denetim Raporu

| Alan | Değer |
|---|---|
| Proje | AppointMan |
| Kapsam | `AppointMan.Api`, `AppointMan.Application`, `AppointMan.Domain`, `AppointMan.Infrastructure` |
| Denetim tarihi | 2025 (commit `f0e254c`) |
| Hedef framework | `net10.0` |
| Referans standartlar | OWASP ASVS L2, OWASP API Security Top 10 (2023), CWE Top 25, Microsoft .NET Security Guidelines |
| Denetim tipi | Statik kaynak kod incelemesi (white-box), salt-okunur |
| Rapor sürümü | 1.0 |

> Bu rapor **yalnızca kodda doğrulanmış** bulguları içerir. Her bulgu `dosya:satır` kanıtıyla desteklenir.
> Doğrulanamayan veya bu denetimin erişemediği alanlar §4'te açıkça listelenmiştir.

---

## 1. Yönetici Özeti ve Genel Risk Duruşu

### 1.1 Özet

AppointMan.Api, katmanlı (Api → Application → Domain → Infrastructure) bir Clean Architecture yapısı üzerinde CQRS/MediatR, FluentValidation ve EF Core kullanmaktadır. Güvenlik açısından **kriptografik ilkeller doğru seçilmiştir**: JWT imzalama HS256 ile yapılmakta, refresh token 64 byte CSPRNG ile üretilmekte, parolalar BCrypt ile hash'lenmekte ve token doğrulamasında `ClockSkew` sıfıra çekilmiştir.

Buna karşılık, **kimlik ve yetki yaşam döngüsü kontrolleri eksik veya stub durumdadır.** En kritik iki sorun:

1. **E-posta doğrulama kodu sabit `"123456"`** ve doğrulanacak kullanıcı `userId` istemciden gelmektedir. Herhangi bir kimliği doğrulanmış kullanıcı, herhangi bir hesabı doğrulanmış duruma getirebilir.
2. **Kayıt akışında rol doğrulanmadan kabul edilmektedir.** İstemci `Role` alanına `Admin` yazarsa, hesap doğrudan yönetici olarak oluşturulur (yetki yükseltme).

Bu iki bulgu tek başına uygulamanın yetki modelini etkisiz kılmaktadır.

### 1.2 Genel risk duruşu: **ZAYIF**

| Katman | Durum | Gerekçe |
|---|---|---|
| Kriptografi (ilkel seçimi) | **İyi** | HS256 + uzun anahtar, BCrypt auto-salt, CSPRNG refresh token |
| Sır yönetimi | **Zayıf** | JWT anahtarı ve connection string kaynak kodda düz metin |
| Kimlik doğrulama yaşam döngüsü | **Zayıf** | Refresh token düz metin, reuse detection yok, lockout yok |
| Yetkilendirme | **Kritik** | Rol whitelist yok, 403 eşlemesi yok, policy tanımı yok |
| Transport / tarayıcı güvenliği | **Zayıf** | HSTS, CORS, güvenlik başlıkları, ForwardedHeaders yok |
| Dayanıklılık (DoS) | **Zayıf** | Rate limiting, request boyut limiti, JSON derinlik limiti yok |
| Hata yönetimi | **Orta** | Stack trace sızmıyor; ancak iç istisna mesajı `Result` içinde taşınıyor |
| Tedarik zinciri / CI | **Zayıf** | Gereksiz paketler, zafiyet taraması ve CI güvenlik adımı yok |

### 1.3 Öncelik dağılımı

| Önem | Adet | Bulgu kimlikleri |
|---|---|---|
| Kritik | 2 | B-01, B-02 |
| Yüksek | 6 | B-03, B-04, B-05, B-06, B-07, B-08 |
| Orta | 10 | B-09 … B-18 |
| Düşük | 4 | B-19 … B-22 |

---

## 2. Bulgu Tablosu

| # | Başlık | CWE / OWASP | Önem | Kanıt (dosya:satır) | Risk senaryosu | Önerilen düzeltme | Efor |
|---|---|---|---|---|---|---|---|
| **B-01** | E-posta doğrulama kodu sabit `"123456"` ve `userId` istemciden alınıyor | CWE-798, CWE-287 · API2, API1 | **Kritik** | `AppointMan.Application\Features\Users\Commands\VerifyEmail\VerifyEmailCommandHandler.cs:37`, `:27` | Saldırgan kayıt olur, `POST /api/auth/verify-email` isteğinde `userId` olarak kurbanın kimliğini ve kod olarak `123456` gönderir. Kurbanın e-postası "doğrulanmış" olur; kurbanın adına doğrulama gerektiren tüm işlemler açılır. Kod hiçbir yerde üretilmediği için doğrulama mekanizması fiilen yoktur. | Kod, kriptografik RNG ile üretilmeli (`RandomNumberGenerator`), **hash'lenerek** DB/cache'te TTL ile saklanmalı, tek kullanımlık olmalı, deneme sayısı sınırlandırılmalı. `userId` **token'dan** alınmalı, gövdeden asla. Sabit `"123456"` kaldırılmalı. | M |
| **B-02** | Kayıt akışında istemci kontrollü rol (yetki yükseltme) | CWE-269, CWE-915 · API6, API5 | **Kritik** | `AppointMan.Application\Features\Users\Commands\RegisterUser\RegisterUserCommandHandler.cs:53` → `AppointMan.Domain\Entities\User.cs:64-83` | `POST /api/auth/register` gövdesinde `"role": "Admin"` (veya enum karşılığı) gönderilir; `User` entity'si rolü doğrudan `Role = role` ile atar (`User.cs:76`). Saldırgan yönetici hesabı elde eder. | `EnumUserRole` üzerinde sunucu tarafı whitelist: yalnızca `Customer` / `BusinessOwner`. Doğrulama **hem** FluentValidation validatöründe **hem** handler içinde (defense in depth). `Admin` ve tanımsız değerler reddedilir. | S |
| **B-03** | Refresh token veritabanında düz metin saklanıyor | CWE-256, CWE-522 · API2 | **Yüksek** | `AppointMan.Domain\Entities\User.cs:28`, `:185-190`; `AppointMan.Application\Features\Users\Commands\Login\LoginCommandHandler.cs:39` | DB yedeği, log, destek ekranı veya SQL injection ile `Users.RefreshToken` kolonuna erişen saldırgan, kullanıcı parolası olmadan süresiz oturum açar. | Token'ın **yalnızca SHA-256 hash'i** saklanmalı (`RefreshTokenHash`). Karşılaştırma sabit zamanlı olmalı. Kolon adı ve tipi migration ile güncellenmeli. | M |
| **B-04** | Refresh token reuse detection ve iptal (revoke) yok | CWE-613, CWE-384 · API2 | **Yüksek** | `AppointMan.Application\Features\Users\Commands\RefreshToken\RefreshTokenCommandHandler.cs:49-58` | Çalınan refresh token yenilenir; meşru kullanıcı da aynı token'ı kullanmaya devam eder. Sunucu ikinci kullanımı ayırt edemez, hırsızlık tespit edilemez ve eski token geçersiz kılınmaz. Çıkış (logout) ve parola değişimi token'ları iptal etmez. | Rotasyon sonrası eski token `PreviousRefreshTokenHash`'e taşınmalı; bu hash tekrar sunulursa **tüm oturumlar iptal** edilip güvenlik logu yazılmalı. `RevokeAllAsync` ile logout ve parola değişiminde iptal. | M |
| **B-05** | Süresi geçmiş token doğrulamasında issuer/audience/lifetime kontrolü kapalı | CWE-345, CWE-613 · API2 | **Yüksek** | `AppointMan.Application\Common\Services\TokenService.cs:60-70` | `GetPrincipalFromExpiredToken` yalnızca imzayı doğrular; `ValidateIssuer = false`, `ValidateAudience = false`, `ValidateLifetime = false`. Aynı anahtarla imzalanmış farklı bir uygulamanın token'ı veya farklı audience için üretilmiş token refresh akışında kabul edilir. | `ValidateIssuer = true`, `ValidateAudience = true` (yalnızca `ValidateLifetime = false` bırakılır, süresi dolmuş access token'ı okumak için). `ValidAlgorithms` yalnızca `HmacSha256` olarak kısıtlanmalı. | S |
| **B-06** | JWT imza anahtarı ve connection string kaynak kodda düz metin | CWE-798, CWE-522 · API8 | **Yüksek** | `AppointMan.Api\appsettings.json:6`, `:3` | Anahtar git geçmişinde kalıcıdır. Repoya erişen herkes tüm ortamlar için geçerli, istediği kullanıcı/rol için token üretebilir. | Anahtar `appsettings.json`'dan çıkarılmalı; environment variable / user-secrets / Key Vault üzerinden beslenmeli. Anahtar **rotasyona** sokulmalı (mevcut değer artık yanmış sayılır). Startup'ta fail-fast doğrulaması eklenmeli. | S |
| **B-07** | Brute-force ve hesap kilitleme (lockout) yok | CWE-307 · API2 | **Yüksek** | `AppointMan.Application\Features\Users\Commands\Login\LoginCommandHandler.cs:33` | `POST /api/auth/login` sınırsız deneme kabul eder. Zayıf parolalı hesaplar sözlük saldırısıyla ele geçirilir; `User` entity'sinde `AccessFailedCount`/`LockoutEnd` alanı yoktur, Identity lockout yapılandırılmamıştır. | Identity `Lockout` seçenekleri (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 dk`) + handler'da başarısız deneme sayacı. Başarısız girişte 423/429 ve genel mesaj (kullanıcı numaralandırmasını önlemek için). | M |
| **B-08** | Rate limiting ve request boyut limiti yok | CWE-770 · API4 | **Yüksek** | `AppointMan.Api\Program.cs:98-115` (pipeline'da `UseRateLimiter` yok) | `/api/auth/login` ve `/api/auth/refresh` endpoint'leri saniyede binlerce istekle dövülerek hem brute-force hem CPU/DB tüketimi (DoS) saldırısına açıktır. Büyük gövdeler belleği tüketir; derin JSON ile stack tüketimi mümkündür. | `AddRateLimiter` + global ve `auth` politikaları; `Kestrel:Limits:MaxRequestBodySize`; `JsonSerializerOptions.MaxDepth`. 429 + `Retry-After` yanıtı. | M |
| **B-09** | Güvenlik başlıkları, HSTS ve `ForwardedHeaders` yok | CWE-693, CWE-319, CWE-1021 · API8 | **Orta** | `AppointMan.Api\Program.cs:98-115`; grep: `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `UseHsts`, `UseForwardedHeaders` → 0 eşleşme | Tarayıcı tabanlı istemcilerde MIME sniffing, clickjacking ve referrer sızıntısı mümkün. Reverse proxy arkasında `RemoteIpAddress` proxy IP'si olur; IP bazlı rate limit/lockout atlatılabilir. | `SecurityHeadersMiddleware` (CSP, nosniff, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`) + production'da `UseHsts` + `UseForwardedHeaders` (KnownProxies/KnownNetworks yapılandırılmış). | S |
| **B-10** | CORS politikası tanımlı değil | CWE-942 · API8 | **Orta** | `AppointMan.Api\Program.cs:98-115` (`AddCors`/`UseCors` yok) | Tarayıcı tabanlı bir istemci eklendiğinde geliştiriciler hızlı çözüm olarak `AllowAnyOrigin` eklemeye yatkındır; o noktada token sızıntısı riski doğar. Şu an tanımsız olması isteklerin reddedilmesine ve geçici `*` çözümlerine yol açar. | `CorsSettings` ile environment bazlı **somut origin listesi**; `AllowAnyOrigin` + `AllowCredentials` kombinasyonu yasak; gerekli header/metod listesi. | S |
| **B-11** | İç istisna mesajı `Result` içinde istemciye taşınıyor | CWE-209 · API8 | **Orta** | `RegisterUserCommandHandler.cs:97`, `LoginCommandHandler.cs:62`, `RefreshTokenCommandHandler.cs:71`, `VerifyEmailCommandHandler.cs:54` — hepsi `Error.Internal(..., ex.Message)` | DB bağlantı dizesi, SQL parçası, dosya yolu veya iç servis adları hata yanıtında görünebilir. Saldırgan hata mesajlarından altyapıyı haritalar. | `Error.Internal` çağrılarından `ex.Message` kaldırılmalı; iç detay yalnız `ILogger.LogError` ile, `correlationId` eşliğinde loglanmalı. | S |
| **B-12** | `AllowedHosts` joker (`*`) | CWE-16 · API8 | **Orta** | `AppointMan.Api\appsettings.json:17` | Host header injection saldırılarına ve DNS rebinding'e karşı koruma yok; parola sıfırlama/doğrulama bağlantıları ileride host'tan türetilirse toksik bağlantı üretilebilir. | Environment bazlı somut host listesi; production'da boşsa fail-fast. | S |
| **B-13** | Production'da otomatik migration + seed | CWE-1188 · API8 | **Orta** | `AppointMan.Api\Program.cs:118-127` | Her başlangıçta `MigrateAsync` ve seed çalışır. Beklenmeyen şema değişikliği, eşzamanlı instance'larda çakışma, seed ile production'a test verisi yazılması riski. | Migration/seed yalnızca `IsDevelopment()` altında veya ayrı bir dağıtım adımında çalıştırılmalı. | S |
| **B-14** | Konfigürasyon fail-fast değil (`!` null-forgiving) | CWE-1188 · API8 | **Orta** | `AppointMan.Api\Program.cs:47` — `Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)` | `JwtSettings:Secret` yoksa uygulama `NullReferenceException` ile anlaşılmaz biçimde çöker; zayıf (kısa) anahtar hiç kontrol edilmez. | `JwtSettings` options sınıfı + `ValidateDataAnnotations()` + `.Validate(key => byte count >= 32)` + `.ValidateOnStart()`. | S |
| **B-15** | `appsettings.Development.json` yok; geliştirme sırları dosyada | CWE-522 · API8 | **Orta** | `AppointMan.Api` dizin listesi: yalnız `appsettings.json`; sırlar `appsettings.json:6` | Temiz clone çalışmaz; geliştiriciler sırları paylaşılan `appsettings.json`'a yazmaya devam eder. `UserSecretsId` tanımlı (`AppointMan.Api.csproj:7`) ancak kullanılmıyor. | Sır içermeyen `appsettings.Development.json` oluşturulmalı; gerçek değerler `dotnet user-secrets` üzerinden. `.gitignore` girdileri doğrulanmalı. | S |
| **B-16** | Connection string'de `TrustServerCertificate=True` | CWE-295 · API8 | **Orta** | `AppointMan.Api\appsettings.json:3` | TLS sertifika doğrulaması devre dışı; aradaki saldırgan (MITM) SQL trafiğini okuyabilir/değiştirebilir. | Yalnızca development'ta bırakılmalı; production connection string'inden kaldırılmalı ve geçerli sertifika zinciri kullanılmalı. | S |
| **B-17** | `Guid.Parse` doğrulanmamış claim üzerinde | CWE-20 · API8 | **Orta** | `RefreshTokenCommandHandler.cs:41` | Zarar görmüş/uyumsuz `userId` claim'i `FormatException` fırlatır; genel `catch` bunu 500'e çevirir ve hata mesajı `Result`'a taşınır. | `Guid.TryParse` kullanılmalı; başarısızsa 401 dönülmeli. | S |
| **B-18** | İş kuralı/authorization hataları için 403 eşlemesi yok; controller'lar 400 dönüyor | CWE-863 · API5 | **Orta** | `GlobalExceptionMiddleware.cs:67-77` (yalnız `UnauthorizedAccessException → 401`, `DomainException → 400`); `BlacklistController.cs:47`, `:81`, `:104` (`BadRequest` her hata için) | Sahiplik/yetki ihlalleri 400 olarak döner; istemci "yetkin yok" ile "girdi hatalı"yı ayırt edemez, izleme (monitoring) yetki ihlallerini kaçırır. 404 dönmesi gereken "bulunamadı" durumları da 400'e düşer. | `ForbiddenAccessException` eklenip 403'e, `NotFoundException` 404'e eşlenmeli. Controller'larda `Result` durum koduna göre `NotFound()` / `Forbid()` / `Problem()` dönülmeli. | M |
| **B-19** | `SaveToken = true` gereksiz | CWE-522 · API2 | **Düşük** | `AppointMan.Api\Program.cs:57` | Token'ın `HttpContext` üzerinden erişilebilir kopyası tutulur; API cookie kullanmadığı için faydası yoktur, bellek/log yüzeyini büyütür. | Kaldırılmalı. | S |
| **B-20** | Gereksiz / riskli NuGet paketleri | CWE-1104 · API9 | **Düşük** | `AppointMan.Api.csproj:21` (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), `:28` (`VisualStudio.Azure.Containers.Tools.Targets`), `:30` (`System.IdentityModel.Tokens.Jwt` 8.22.0), `:31` (`System.Security.Cryptography.Pkcs`) | Kullanılmayan paketler saldırı yüzeyini ve güncelleme yükünü artırır; açık `System.IdentityModel.Tokens.Jwt` referansı framework sürümüyle çakışabilir (sürüm kayması). | Kullanılmayanlar kaldırılmalı; JWT paketi tek sürüme sabitlenmeli veya framework'ün transitif sürümüne bırakılmalı. | S |
| **B-21** | Refresh token süresi ve access token süresi kod içinde sabit | CWE-613 · API2 | **Düşük** | `LoginCommandHandler.cs:39`, `:47`; `RefreshTokenCommandHandler.cs:57`, `:64` | `AddDays(7)` / `AddMinutes(60)` değerleri konfigürasyondan bağımsız; `JwtSettings:ExpirationInMinutes` ile tutarsız (token 60 dk geçerli ama response 60 dk, refresh 7 gün sabit). Sözleşme değişince sessiz sapma oluşur. | Süreler `JwtSettings` üzerinden okunmalı, tek kaynaktan yönetilmeli. | S |
| **B-22** | CI güvenlik taraması yok | CWE-1053 · API9 | **Düşük** | `.github/workflows/` dizini boş | Zafiyetli bağımlılık veya secret sızıntısı otomatik yakalanmaz. | `dotnet list package --vulnerable --include-transitive` + secret scanning + CodeQL adımları içeren workflow. | S |

---

## 3. İyi Durumdaki Kontroller (tekrar iş yapılmayacak)

Aşağıdaki kontroller doğru uygulanmıştır; düzeltme kapsamına **alınmamalıdır**.

| Kontrol | Kanıt | Not |
|---|---|---|
| HS256 imzalama + `SymmetricSecurityKey` | `TokenService.cs:24-25`, `Program.cs:66` | Algoritma seçimi doğru |
| `ValidateIssuer` / `ValidateAudience` / `ValidateLifetime` / `ValidateIssuerSigningKey` | `Program.cs:60-63` | Access token doğrulaması eksiksiz |
| `ClockSkew = TimeSpan.Zero` | `Program.cs:67` | Sıkı, iyi |
| Refresh token üretimi CSPRNG ile 64 byte | `TokenService.cs:52-58` | Tahmin edilemez; korunmalı |
| Parola hash'leme BCrypt + otomatik salt | `RegisterUserCommandHandler.cs:56`, `LoginCommandHandler.cs:33` | BCrypt.Net-Next 4.2.0 |
| `RequireHttpsMetadata = true` | `Program.cs:56` | Korunmalı (forwarded headers ile birlikte anlam kazanır) |
| Swagger yalnız development'ta | `Program.cs:100-104` | Production'da açık değil |
| Refresh handler'da süre kontrolü | `RefreshTokenCommandHandler.cs:49` | `RefreshTokenExpiryTime <= UtcNow` kontrolü mevcut |
| Controller'da sunucu tarafı kimlik çözümü | `BlacklistController.cs:36-40`, `:92-97`, `:107-116` | `RequesterId` istemciden değil claim'den alınıp komuta yazılıyor — **doğru desen**, diğer controller'lara örnek olmalı |
| FluentValidation + `ValidationBehavior` pipeline'ı | `AppointMan.Application\Common\Behaviors\ValidationBehavior.cs` | Hata yönetimi ile entegre |
| Domain seviyesinde sahiplik kontrolleri | `User.cs:321-322`, `:333-334`, `:345-346`, `:375-376` | `AddAppointment`/`AddReview`/`AddFavorite`/`AddNotification` sahiplik doğruluyor |
| Stack trace istemciye gönderilmiyor | `GlobalExceptionMiddleware.cs:157-169` | `Detail` yalnız development'ta |
| `UserSecretsId` tanımlı | `AppointMan.Api.csproj:7` | Altyapı hazır, kullanılması yeterli |
| Domain değer nesneleri ile girdi doğrulama | `Email`, `PhoneNumber` value object'leri (`RegisterUserCommandHandler.cs:30`, `:39`) | Tip güvenli doğrulama |

---

## 4. Kapsam Dışı ve Doğrulanamayan Alanlar

Bu denetimde aşağıdaki alanlar **incelenmemiştir** veya **erişilememiştir**; bunlar için ayrı doğrulama gerekir.

| Alan | Neden |
|---|---|
| Canlı ortam sırları ve secret store | Azure Key Vault / production environment değişkenleri repodan görülemez |
| Git geçmişi sır taraması | Yalnız mevcut dosya ağacı incelendi. `gitleaks` / `trufflehog` ile geçmiş taraması önerilir |
| `.env`, `docker-compose.yml`, `docker-compose.override.yml.backup`, `Dockerfile` içerikleri | Bu oturumda satır bazında doğrulanmadı; default parola/imaj sırları için ayrıca incelenmeli |
| SQL Server kullanıcı yetkileri | DB principal/rol yapılandırması repoda yok; "least privilege" ayrı kontrol edilmeli |
| TLS termination / sertifika zinciri | Reverse proxy veya ingress katmanı bu çözümde değil |
| Tüm controller'larda BOLA/IDOR taraması | `BlacklistController` örnek olarak tam incelendi; `AppointmentsController`, `BusinessController`, `ServiceController`, `ReviewController`, `FavoriteController`, `EmployeeController`, `UserController`, `NotificationController`, `WorkingHourController`, `AddressController`, `AppointmentHistoryController`, `BusinessCategoryController`, `ServiceCategoryController` için **handler seviyesinde sahiplik doğrulaması ayrıca denetlenmeli** |
| Üçüncü parti servis tüketimi (API10) | Dış API çağrısı yapan kod tespit edilmedi; tespit edilirse timeout/TLS doğrulaması ayrı bulgu olarak eklenmeli |
| MFA / e-posta doğrulama akışının tamamı | Yalnız mevcut stub doğrulandı; SMTP ve token üretimi akışı yok |
| SignalR | Hub tespit edilmedi |
| Penetrasyon testi / dinamik analiz (DAST) | Bu rapor statik incelemedir; ZAP/Burp ile doğrulama önerilir |

---

## 5. Doğrulama Stratejisi Özeti

Her bulgu için düzeltme sonrası yapılacak manuel doğrulama. Tüm komutlar `curl` (PowerShell: `curl.exe`) ile çalıştırılabilir.

| # | Doğrulama | Beklenen sonuç |
|---|---|---|
| B-01 | `POST /api/auth/verify-email` gövdede `{"userId":"<başka kullanıcı>","verificationCode":"123456"}` | `400`/`401` — sabit kod reddedilir, `userId` gövdesi yok sayılır |
| B-01 | Kod üretimi → 5 hatalı deneme → 6. deneme | `429`/`400` — deneme limiti |
| B-02 | `POST /api/auth/register` gövdede `"role":"Admin"` | `400` validasyon hatası; hiçbir kullanıcı `Admin` olarak yaratılmaz |
| B-02 | `"role":"Customer"` ile kayıt | `200`/`201`, DB'de rol `Customer` |
| B-03 | Kayıt/giriş sonrası DB sorgusu: `SELECT RefreshTokenHash FROM Users` | Düz token görünmez; Base64 SHA-256 hash |
| B-04 | Refresh → eski refresh token ile tekrar refresh | `401` + kullanıcının tüm oturumları iptal + `Warning` log |
| B-04 | Logout sonrası refresh | `401` |
| B-05 | Farklı `Audience` ile imzalanmış token ile refresh | `401` |
| B-06 | `appsettings.json` içinde `Secret` alanı | Boş/`""`; `git grep -n -i "secret\|password\|jwtkey"` sonuç vermez |
| B-06 | Env değişkeni olmadan uygulamayı başlat | `OptionsValidationException` — uygulama **başlamaz** |
| B-07 | 5 kez yanlış parola → 6. deneme | Hesap kilitli (`423`/`429`), doğru parola ile de giriş yapılamaz (kilit süresince) |
| B-08 | `/api/auth/login` 10 hızlı istek | `429` + `Retry-After` başlığı |
| B-08 | 10 MB gövde gönderimi | `413` |
| B-09 | `curl -I` ile herhangi bir endpoint | `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` başlıkları mevcut; production'da `Strict-Transport-Security` |
| B-10 | İzinsiz `Origin` ile preflight | CORS başlıkları dönmez; izinli origin'de döner |
| B-11 | Kasıtlı DB hatası tetikle | Yanıtta iç mesaj/SQL yok; yalnız genel mesaj + `correlationId` |
| B-12 | `Host: evil.example` başlığı ile istek | `400` (AllowedHosts reddi) |
| B-13 | Production konfigürasyonu ile başlatma | Migration/seed çalışmaz; şema değişmez |
| B-14 | 16 karakterlik `Jwt:Secret` ile başlatma | Doğrulama hatası — uygulama başlamaz |
| B-17 | Bozuk `userId` claim'i içeren token ile refresh | `401` (500 değil) |
| B-18 | A kullanıcısı ile B'nin kaynağına erişim | `403` veya `404` (400 değil) |
| B-18 | Var olmayan id | `404` (400 değil) |
| B-20 | `dotnet list package --vulnerable --include-transitive` | Zafiyet yok |
| B-22 | CI workflow tetikleme | Build + zafiyet taraması yeşil |

---

## 6. Sonraki Adımlar

Bu rapor, uygulama planının 1. adımıdır ve **onay kapısıdır**. Onay sonrası sırasıyla:

1. Konfigürasyon ve sır yönetimi sertleştirmesi (B-06, B-12, B-14, B-15, B-16)
2. Kimlik doğrulama yaşam döngüsü (B-01, B-02, B-03, B-04, B-05, B-07, B-17, B-19, B-21)
3. Yetkilendirme ve BOLA/IDOR (B-18 + §4'te listelenen controller'ların tamamı)
4. Transport ve tarayıcı güvenliği (B-09, B-10)
5. Rate limiting, hata ve log hijyeni (B-08, B-11, B-13)
6. Girdi doğrulama, bağımlılık ve CI/altyapı temizliği (B-20, B-22)

> **Not:** B-01 ve B-02 canlı ortamda sömürülebilir niteliktedir. Düzeltmeler tamamlanana kadar, etkilenen endpoint'ler (`verify-email`, `register`) için erişim kısıtlaması veya geçici devre dışı bırakma değerlendirilmelidir.
