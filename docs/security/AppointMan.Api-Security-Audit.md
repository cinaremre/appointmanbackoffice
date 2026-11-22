# Rezervise.Api — Güvenlik Katmani Denetim Raporu

| Alan | Deger |
|---|---|
| Proje | Rezervise |
| Kapsam | `Rezervise.Api`, `Rezervise.Application`, `Rezervise.Domain`, `Rezervise.Infrastructure` |
| Denetim tarihi | 2025 (commit `f0e254c`) |
| Hedef framework | `net10.0` |
| Referans standartlar | OWASP ASVS L2, OWASP API Security Top 10 (2023), CWE Top 25, Microsoft .NET Security Guidelines |
| Denetim tipi | Statik kaynak kod incelemesi (white-box), salt-okunur |
| Rapor sürümü | 1.0 |

> Bu rapor **yalnizca kodda dogrulanmis** bulgulari içerir. Her bulgu `dosya:satir` kanitiyla desteklenir.
> Dogrulanamayan veya bu denetimin erisemedigi alanlar §4'te açikça listelenmistir.

---

## 1. Yönetici Özeti ve Genel Risk Durusu

### 1.1 Özet

Rezervise.Api, katmanli (Api ? Application ? Domain ? Infrastructure) bir Clean Architecture yapisi üzerinde CQRS/MediatR, FluentValidation ve EF Core kullanmaktadir. Güvenlik açisindan **kriptografik ilkeller dogru seçilmistir**: JWT imzalama HS256 ile yapilmakta, refresh token 64 byte CSPRNG ile üretilmekte, parolalar BCrypt ile hash'lenmekte ve token dogrulamasinda `ClockSkew` sifira çekilmistir.

Buna karsilik, **kimlik ve yetki yasam döngüsü kontrolleri eksik veya stub durumdadir.** En kritik iki sorun:

1. **E-posta dogrulama kodu sabit `"123456"`** ve dogrulanacak kullanici `userId` istemciden gelmektedir. Herhangi bir kimligi dogrulanmis kullanici, herhangi bir hesabi dogrulanmis duruma getirebilir.
2. **Kayit akisinda rol dogrulanmadan kabul edilmektedir.** Istemci `Role` alanina `Admin` yazarsa, hesap dogrudan yönetici olarak olusturulur (yetki yükseltme).

Bu iki bulgu tek basina uygulamanin yetki modelini etkisiz kilmaktadir.

### 1.2 Genel risk durusu: **ZAYIF**

| Katman | Durum | Gerekçe |
|---|---|---|
| Kriptografi (ilkel seçimi) | **Iyi** | HS256 + uzun anahtar, BCrypt auto-salt, CSPRNG refresh token |
| Sir yönetimi | **Zayif** | JWT anahtari ve connection string kaynak kodda düz metin |
| Kimlik dogrulama yasam döngüsü | **Zayif** | Refresh token düz metin, reuse detection yok, lockout yok |
| Yetkilendirme | **Kritik** | Rol whitelist yok, 403 eslemesi yok, policy tanimi yok |
| Transport / tarayici güvenligi | **Zayif** | HSTS, CORS, güvenlik basliklari, ForwardedHeaders yok |
| Dayaniklilik (DoS) | **Zayif** | Rate limiting, request boyut limiti, JSON derinlik limiti yok |
| Hata yönetimi | **Orta** | Stack trace sizmiyor; ancak iç istisna mesaji `Result` içinde tasiniyor |
| Tedarik zinciri / CI | **Zayif** | Gereksiz paketler, zafiyet taramasi ve CI güvenlik adimi yok |

### 1.3 Öncelik dagilimi

| Önem | Adet | Bulgu kimlikleri |
|---|---|---|
| Kritik | 2 | B-01, B-02 |
| Yüksek | 6 | B-03, B-04, B-05, B-06, B-07, B-08 |
| Orta | 10 | B-09 … B-18 |
| Düsük | 4 | B-19 … B-22 |

---

## 2. Bulgu Tablosu

| # | Baslik | CWE / OWASP | Önem | Kanit (dosya:satir) | Risk senaryosu | Önerilen düzeltme | Efor |
|---|---|---|---|---|---|---|---|
| **B-01** | E-posta dogrulama kodu sabit `"123456"` ve `userId` istemciden aliniyor | CWE-798, CWE-287 · API2, API1 | **Kritik** | `Rezervise.Application\Features\Users\Commands\VerifyEmail\VerifyEmailCommandHandler.cs:37`, `:27` | Saldirgan kayit olur, `POST /api/auth/verify-email` isteginde `userId` olarak kurbanin kimligini ve kod olarak `123456` gönderir. Kurbanin e-postasi "dogrulanmis" olur; kurbanin adina dogrulama gerektiren tüm islemler açilir. Kod hiçbir yerde üretilmedigi için dogrulama mekanizmasi fiilen yoktur. | Kod, kriptografik RNG ile üretilmeli (`RandomNumberGenerator`), **hash'lenerek** DB/cache'te TTL ile saklanmali, tek kullanimlik olmali, deneme sayisi sinirlandirilmali. `userId` **token'dan** alinmali, gövdeden asla. Sabit `"123456"` kaldirilmali. | M |
| **B-02** | Kayit akisinda istemci kontrollü rol (yetki yükseltme) | CWE-269, CWE-915 · API6, API5 | **Kritik** | `Rezervise.Application\Features\Users\Commands\RegisterUser\RegisterUserCommandHandler.cs:53` ? `Rezervise.Domain\Entities\User.cs:64-83` | `POST /api/auth/register` gövdesinde `"role": "Admin"` (veya enum karsiligi) gönderilir; `User` entity'si rolü dogrudan `Role = role` ile atar (`User.cs:76`). Saldirgan yönetici hesabi elde eder. | `EnumUserRole` üzerinde sunucu tarafi whitelist: yalnizca `Customer` / `BusinessOwner`. Dogrulama **hem** FluentValidation validatöründe **hem** handler içinde (defense in depth). `Admin` ve tanimsiz degerler reddedilir. | S |
| **B-03** | Refresh token veritabaninda düz metin saklaniyor | CWE-256, CWE-522 · API2 | **Yüksek** | `Rezervise.Domain\Entities\User.cs:28`, `:185-190`; `Rezervise.Application\Features\Users\Commands\Login\LoginCommandHandler.cs:39` | DB yedegi, log, destek ekrani veya SQL injection ile `Users.RefreshToken` kolonuna erisen saldirgan, kullanici parolasi olmadan süresiz oturum açar. | Token'in **yalnizca SHA-256 hash'i** saklanmali (`RefreshTokenHash`). Karsilastirma sabit zamanli olmali. Kolon adi ve tipi migration ile güncellenmeli. | M |
| **B-04** | Refresh token reuse detection ve iptal (revoke) yok | CWE-613, CWE-384 · API2 | **Yüksek** | `Rezervise.Application\Features\Users\Commands\RefreshToken\RefreshTokenCommandHandler.cs:49-58` | Çalinan refresh token yenilenir; mesru kullanici da ayni token'i kullanmaya devam eder. Sunucu ikinci kullanimi ayirt edemez, hirsizlik tespit edilemez ve eski token geçersiz kilinmaz. Çikis (logout) ve parola degisimi token'lari iptal etmez. | Rotasyon sonrasi eski token `PreviousRefreshTokenHash`'e tasinmali; bu hash tekrar sunulursa **tüm oturumlar iptal** edilip güvenlik logu yazilmali. `RevokeAllAsync` ile logout ve parola degisiminde iptal. | M |
| **B-05** | Süresi geçmis token dogrulamasinda issuer/audience/lifetime kontrolü kapali | CWE-345, CWE-613 · API2 | **Yüksek** | `Rezervise.Application\Common\Services\TokenService.cs:60-70` | `GetPrincipalFromExpiredToken` yalnizca imzayi dogrular; `ValidateIssuer = false`, `ValidateAudience = false`, `ValidateLifetime = false`. Ayni anahtarla imzalanmis farkli bir uygulamanin token'i veya farkli audience için üretilmis token refresh akisinda kabul edilir. | `ValidateIssuer = true`, `ValidateAudience = true` (yalnizca `ValidateLifetime = false` birakilir, süresi dolmus access token'i okumak için). `ValidAlgorithms` yalnizca `HmacSha256` olarak kisitlanmali. | S |
| **B-06** | JWT imza anahtari ve connection string kaynak kodda düz metin | CWE-798, CWE-522 · API8 | **Yüksek** | `Rezervise.Api\appsettings.json:6`, `:3` | Anahtar git geçmisinde kalicidir. Repoya erisen herkes tüm ortamlar için geçerli, istedigi kullanici/rol için token üretebilir. | Anahtar `appsettings.json`'dan çikarilmali; environment variable / user-secrets / Key Vault üzerinden beslenmeli. Anahtar **rotasyona** sokulmali (mevcut deger artik yanmis sayilir). Startup'ta fail-fast dogrulamasi eklenmeli. | S |
| **B-07** | Brute-force ve hesap kilitleme (lockout) yok | CWE-307 · API2 | **Yüksek** | `Rezervise.Application\Features\Users\Commands\Login\LoginCommandHandler.cs:33` | `POST /api/auth/login` sinirsiz deneme kabul eder. Zayif parolali hesaplar sözlük saldirisiyla ele geçirilir; `User` entity'sinde `AccessFailedCount`/`LockoutEnd` alani yoktur, Identity lockout yapilandirilmamistir. | Identity `Lockout` seçenekleri (`MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 dk`) + handler'da basarisiz deneme sayaci. Basarisiz giriste 423/429 ve genel mesaj (kullanici numaralandirmasini önlemek için). | M |
| **B-08** | Rate limiting ve request boyut limiti yok | CWE-770 · API4 | **Yüksek** | `Rezervise.Api\Program.cs:98-115` (pipeline'da `UseRateLimiter` yok) | `/api/auth/login` ve `/api/auth/refresh` endpoint'leri saniyede binlerce istekle dövülerek hem brute-force hem CPU/DB tüketimi (DoS) saldirisina açiktir. Büyük gövdeler bellegi tüketir; derin JSON ile stack tüketimi mümkündür. | `AddRateLimiter` + global ve `auth` politikalari; `Kestrel:Limits:MaxRequestBodySize`; `JsonSerializerOptions.MaxDepth`. 429 + `Retry-After` yaniti. | M |
| **B-09** | Güvenlik basliklari, HSTS ve `ForwardedHeaders` yok | CWE-693, CWE-319, CWE-1021 · API8 | **Orta** | `Rezervise.Api\Program.cs:98-115`; grep: `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `UseHsts`, `UseForwardedHeaders` ? 0 eslesme | Tarayici tabanli istemcilerde MIME sniffing, clickjacking ve referrer sizintisi mümkün. Reverse proxy arkasinda `RemoteIpAddress` proxy IP'si olur; IP bazli rate limit/lockout atlatilabilir. | `SecurityHeadersMiddleware` (CSP, nosniff, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`) + production'da `UseHsts` + `UseForwardedHeaders` (KnownProxies/KnownNetworks yapilandirilmis). | S |
| **B-10** | CORS politikasi tanimli degil | CWE-942 · API8 | **Orta** | `Rezervise.Api\Program.cs:98-115` (`AddCors`/`UseCors` yok) | Tarayici tabanli bir istemci eklendiginde gelistiriciler hizli çözüm olarak `AllowAnyOrigin` eklemeye yatkindir; o noktada token sizintisi riski dogar. Su an tanimsiz olmasi isteklerin reddedilmesine ve geçici `*` çözümlerine yol açar. | `CorsSettings` ile environment bazli **somut origin listesi**; `AllowAnyOrigin` + `AllowCredentials` kombinasyonu yasak; gerekli header/metod listesi. | S |
| **B-11** | Iç istisna mesaji `Result` içinde istemciye tasiniyor | CWE-209 · API8 | **Orta** | `RegisterUserCommandHandler.cs:97`, `LoginCommandHandler.cs:62`, `RefreshTokenCommandHandler.cs:71`, `VerifyEmailCommandHandler.cs:54` — hepsi `Error.Internal(..., ex.Message)` | DB baglanti dizesi, SQL parçasi, dosya yolu veya iç servis adlari hata yanitinda görünebilir. Saldirgan hata mesajlarindan altyapiyi haritalar. | `Error.Internal` çagrilarindan `ex.Message` kaldirilmali; iç detay yalniz `ILogger.LogError` ile, `correlationId` esliginde loglanmali. | S |
| **B-12** | `AllowedHosts` joker (`*`) | CWE-16 · API8 | **Orta** | `Rezervise.Api\appsettings.json:17` | Host header injection saldirilarina ve DNS rebinding'e karsi koruma yok; parola sifirlama/dogrulama baglantilari ileride host'tan türetilirse toksik baglanti üretilebilir. | Environment bazli somut host listesi; production'da bossa fail-fast. | S |
| **B-13** | Production'da otomatik migration + seed | CWE-1188 · API8 | **Orta** | `Rezervise.Api\Program.cs:118-127` | Her baslangiçta `MigrateAsync` ve seed çalisir. Beklenmeyen sema degisikligi, eszamanli instance'larda çakisma, seed ile production'a test verisi yazilmasi riski. | Migration/seed yalnizca `IsDevelopment()` altinda veya ayri bir dagitim adiminda çalistirilmali. | S |
| **B-14** | Konfigürasyon fail-fast degil (`!` null-forgiving) | CWE-1188 · API8 | **Orta** | `Rezervise.Api\Program.cs:47` — `Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)` | `JwtSettings:Secret` yoksa uygulama `NullReferenceException` ile anlasilmaz biçimde çöker; zayif (kisa) anahtar hiç kontrol edilmez. | `JwtSettings` options sinifi + `ValidateDataAnnotations()` + `.Validate(key => byte count >= 32)` + `.ValidateOnStart()`. | S |
| **B-15** | `appsettings.Development.json` yok; gelistirme sirlari dosyada | CWE-522 · API8 | **Orta** | `Rezervise.Api` dizin listesi: yalniz `appsettings.json`; sirlar `appsettings.json:6` | Temiz clone çalismaz; gelistiriciler sirlari paylasilan `appsettings.json`'a yazmaya devam eder. `UserSecretsId` tanimli (`Rezervise.Api.csproj:7`) ancak kullanilmiyor. | Sir içermeyen `appsettings.Development.json` olusturulmali; gerçek degerler `dotnet user-secrets` üzerinden. `.gitignore` girdileri dogrulanmali. | S |
| **B-16** | Connection string'de `TrustServerCertificate=True` | CWE-295 · API8 | **Orta** | `Rezervise.Api\appsettings.json:3` | TLS sertifika dogrulamasi devre disi; aradaki saldirgan (MITM) SQL trafigini okuyabilir/degistirebilir. | Yalnizca development'ta birakilmali; production connection string'inden kaldirilmali ve geçerli sertifika zinciri kullanilmali. | S |
| **B-17** | `Guid.Parse` dogrulanmamis claim üzerinde | CWE-20 · API8 | **Orta** | `RefreshTokenCommandHandler.cs:41` | Zarar görmüs/uyumsuz `userId` claim'i `FormatException` firlatir; genel `catch` bunu 500'e çevirir ve hata mesaji `Result`'a tasinir. | `Guid.TryParse` kullanilmali; basarisizsa 401 dönülmeli. | S |
| **B-18** | Is kurali/authorization hatalari için 403 eslemesi yok; controller'lar 400 dönüyor | CWE-863 · API5 | **Orta** | `GlobalExceptionMiddleware.cs:67-77` (yalniz `UnauthorizedAccessException ? 401`, `DomainException ? 400`); `BlacklistController.cs:47`, `:81`, `:104` (`BadRequest` her hata için) | Sahiplik/yetki ihlalleri 400 olarak döner; istemci "yetkin yok" ile "girdi hatali"yi ayirt edemez, izleme (monitoring) yetki ihlallerini kaçirir. 404 dönmesi gereken "bulunamadi" durumlari da 400'e düser. | `ForbiddenAccessException` eklenip 403'e, `NotFoundException` 404'e eslenmeli. Controller'larda `Result` durum koduna göre `NotFound()` / `Forbid()` / `Problem()` dönülmeli. | M |
| **B-19** | `SaveToken = true` gereksiz | CWE-522 · API2 | **Düsük** | `Rezervise.Api\Program.cs:57` | Token'in `HttpContext` üzerinden erisilebilir kopyasi tutulur; API cookie kullanmadigi için faydasi yoktur, bellek/log yüzeyini büyütür. | Kaldirilmali. | S |
| **B-20** | Gereksiz / riskli NuGet paketleri | CWE-1104 · API9 | **Düsük** | `Rezervise.Api.csproj:21` (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`), `:28` (`VisualStudio.Azure.Containers.Tools.Targets`), `:30` (`System.IdentityModel.Tokens.Jwt` 8.22.0), `:31` (`System.Security.Cryptography.Pkcs`) | Kullanilmayan paketler saldiri yüzeyini ve güncelleme yükünü artirir; açik `System.IdentityModel.Tokens.Jwt` referansi framework sürümüyle çakisabilir (sürüm kaymasi). | Kullanilmayanlar kaldirilmali; JWT paketi tek sürüme sabitlenmeli veya framework'ün transitif sürümüne birakilmali. | S |
| **B-21** | Refresh token süresi ve access token süresi kod içinde sabit | CWE-613 · API2 | **Düsük** | `LoginCommandHandler.cs:39`, `:47`; `RefreshTokenCommandHandler.cs:57`, `:64` | `AddDays(7)` / `AddMinutes(60)` degerleri konfigürasyondan bagimsiz; `JwtSettings:ExpirationInMinutes` ile tutarsiz (token 60 dk geçerli ama response 60 dk, refresh 7 gün sabit). Sözlesme degisince sessiz sapma olusur. | Süreler `JwtSettings` üzerinden okunmali, tek kaynaktan yönetilmeli. | S |
| **B-22** | CI güvenlik taramasi yok | CWE-1053 · API9 | **Düsük** | `.github/workflows/` dizini bos | Zafiyetli bagimlilik veya secret sizintisi otomatik yakalanmaz. | `dotnet list package --vulnerable --include-transitive` + secret scanning + CodeQL adimlari içeren workflow. | S |

---

## 3. Iyi Durumdaki Kontroller (tekrar is yapilmayacak)

Asagidaki kontroller dogru uygulanmistir; düzeltme kapsamina **alinmamalidir**.

| Kontrol | Kanit | Not |
|---|---|---|
| HS256 imzalama + `SymmetricSecurityKey` | `TokenService.cs:24-25`, `Program.cs:66` | Algoritma seçimi dogru |
| `ValidateIssuer` / `ValidateAudience` / `ValidateLifetime` / `ValidateIssuerSigningKey` | `Program.cs:60-63` | Access token dogrulamasi eksiksiz |
| `ClockSkew = TimeSpan.Zero` | `Program.cs:67` | Siki, iyi |
| Refresh token üretimi CSPRNG ile 64 byte | `TokenService.cs:52-58` | Tahmin edilemez; korunmali |
| Parola hash'leme BCrypt + otomatik salt | `RegisterUserCommandHandler.cs:56`, `LoginCommandHandler.cs:33` | BCrypt.Net-Next 4.2.0 |
| `RequireHttpsMetadata = true` | `Program.cs:56` | Korunmali (forwarded headers ile birlikte anlam kazanir) |
| Swagger yalniz development'ta | `Program.cs:100-104` | Production'da açik degil |
| Refresh handler'da süre kontrolü | `RefreshTokenCommandHandler.cs:49` | `RefreshTokenExpiryTime <= UtcNow` kontrolü mevcut |
| Controller'da sunucu tarafi kimlik çözümü | `BlacklistController.cs:36-40`, `:92-97`, `:107-116` | `RequesterId` istemciden degil claim'den alinip komuta yaziliyor — **dogru desen**, diger controller'lara örnek olmali |
| FluentValidation + `ValidationBehavior` pipeline'i | `Rezervise.Application\Common\Behaviors\ValidationBehavior.cs` | Hata yönetimi ile entegre |
| Domain seviyesinde sahiplik kontrolleri | `User.cs:321-322`, `:333-334`, `:345-346`, `:375-376` | `AddAppointment`/`AddReview`/`AddFavorite`/`AddNotification` sahiplik dogruluyor |
| Stack trace istemciye gönderilmiyor | `GlobalExceptionMiddleware.cs:157-169` | `Detail` yalniz development'ta |
| `UserSecretsId` tanimli | `Rezervise.Api.csproj:7` | Altyapi hazir, kullanilmasi yeterli |
| Domain deger nesneleri ile girdi dogrulama | `Email`, `PhoneNumber` value object'leri (`RegisterUserCommandHandler.cs:30`, `:39`) | Tip güvenli dogrulama |

---

## 4. Kapsam Disi ve Dogrulanamayan Alanlar

Bu denetimde asagidaki alanlar **incelenmemistir** veya **erisilememistir**; bunlar için ayri dogrulama gerekir.

| Alan | Neden |
|---|---|
| Canli ortam sirlari ve secret store | Azure Key Vault / production environment degiskenleri repodan görülemez |
| Git geçmisi sir taramasi | Yalniz mevcut dosya agaci incelendi. `gitleaks` / `trufflehog` ile geçmis taramasi önerilir |
| `.env`, `docker-compose.yml`, `docker-compose.override.yml.backup`, `Dockerfile` içerikleri | Bu oturumda satir bazinda dogrulanmadi; default parola/imaj sirlari için ayrica incelenmeli |
| SQL Server kullanici yetkileri | DB principal/rol yapilandirmasi repoda yok; "least privilege" ayri kontrol edilmeli |
| TLS termination / sertifika zinciri | Reverse proxy veya ingress katmani bu çözümde degil |
| Tüm controller'larda BOLA/IDOR taramasi | `BlacklistController` örnek olarak tam incelendi; `AppointmentsController`, `BusinessController`, `ServiceController`, `ReviewController`, `FavoriteController`, `EmployeeController`, `UserController`, `NotificationController`, `WorkingHourController`, `AddressController`, `AppointmentHistoryController`, `BusinessCategoryController`, `ServiceCategoryController` için **handler seviyesinde sahiplik dogrulamasi ayrica denetlenmeli** |
| Üçüncü parti servis tüketimi (API10) | Dis API çagrisi yapan kod tespit edilmedi; tespit edilirse timeout/TLS dogrulamasi ayri bulgu olarak eklenmeli |
| MFA / e-posta dogrulama akisinin tamami | Yalniz mevcut stub dogrulandi; SMTP ve token üretimi akisi yok |
| SignalR | Hub tespit edilmedi |
| Penetrasyon testi / dinamik analiz (DAST) | Bu rapor statik incelemedir; ZAP/Burp ile dogrulama önerilir |

---

## 5. Dogrulama Stratejisi Özeti

Her bulgu için düzeltme sonrasi yapilacak manuel dogrulama. Tüm komutlar `curl` (PowerShell: `curl.exe`) ile çalistirilabilir.

| # | Dogrulama | Beklenen sonuç |
|---|---|---|
| B-01 | `POST /api/auth/verify-email` gövdede `{"userId":"<baska kullanici>","verificationCode":"123456"}` | `400`/`401` — sabit kod reddedilir, `userId` gövdesi yok sayilir |
| B-01 | Kod üretimi ? 5 hatali deneme ? 6. deneme | `429`/`400` — deneme limiti |
| B-02 | `POST /api/auth/register` gövdede `"role":"Admin"` | `400` validasyon hatasi; hiçbir kullanici `Admin` olarak yaratilmaz |
| B-02 | `"role":"Customer"` ile kayit | `200`/`201`, DB'de rol `Customer` |
| B-03 | Kayit/giris sonrasi DB sorgusu: `SELECT RefreshTokenHash FROM Users` | Düz token görünmez; Base64 SHA-256 hash |
| B-04 | Refresh ? eski refresh token ile tekrar refresh | `401` + kullanicinin tüm oturumlari iptal + `Warning` log |
| B-04 | Logout sonrasi refresh | `401` |
| B-05 | Farkli `Audience` ile imzalanmis token ile refresh | `401` |
| B-06 | `appsettings.json` içinde `Secret` alani | Bos/`""`; `git grep -n -i "secret\|password\|jwtkey"` sonuç vermez |
| B-06 | Env degiskeni olmadan uygulamayi baslat | `OptionsValidationException` — uygulama **baslamaz** |
| B-07 | 5 kez yanlis parola ? 6. deneme | Hesap kilitli (`423`/`429`), dogru parola ile de giris yapilamaz (kilit süresince) |
| B-08 | `/api/auth/login` 10 hizli istek | `429` + `Retry-After` basligi |
| B-08 | 10 MB gövde gönderimi | `413` |
| B-09 | `curl -I` ile herhangi bir endpoint | `Content-Security-Policy`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` basliklari mevcut; production'da `Strict-Transport-Security` |
| B-10 | Izinsiz `Origin` ile preflight | CORS basliklari dönmez; izinli origin'de döner |
| B-11 | Kasitli DB hatasi tetikle | Yanitta iç mesaj/SQL yok; yalniz genel mesaj + `correlationId` |
| B-12 | `Host: evil.example` basligi ile istek | `400` (AllowedHosts reddi) |
| B-13 | Production konfigürasyonu ile baslatma | Migration/seed çalismaz; sema degismez |
| B-14 | 16 karakterlik `Jwt:Secret` ile baslatma | Dogrulama hatasi — uygulama baslamaz |
| B-17 | Bozuk `userId` claim'i içeren token ile refresh | `401` (500 degil) |
| B-18 | A kullanicisi ile B'nin kaynagina erisim | `403` veya `404` (400 degil) |
| B-18 | Var olmayan id | `404` (400 degil) |
| B-20 | `dotnet list package --vulnerable --include-transitive` | Zafiyet yok |
| B-22 | CI workflow tetikleme | Build + zafiyet taramasi yesil |

---

## 6. Sonraki Adimlar

Bu rapor, uygulama planinin 1. adimidir ve **onay kapisidir**. Onay sonrasi sirasiyla:

1. Konfigürasyon ve sir yönetimi sertlestirmesi (B-06, B-12, B-14, B-15, B-16)
2. Kimlik dogrulama yasam döngüsü (B-01, B-02, B-03, B-04, B-05, B-07, B-17, B-19, B-21)
3. Yetkilendirme ve BOLA/IDOR (B-18 + §4'te listelenen controller'larin tamami)
4. Transport ve tarayici güvenligi (B-09, B-10)
5. Rate limiting, hata ve log hijyeni (B-08, B-11, B-13)
6. Girdi dogrulama, bagimlilik ve CI/altyapi temizligi (B-20, B-22)

> **Not:** B-01 ve B-02 canli ortamda sömürülebilir niteliktedir. Düzeltmeler tamamlanana kadar, etkilenen endpoint'ler (`verify-email`, `register`) için erisim kisitlamasi veya geçici devre disi birakma degerlendirilmelidir.
