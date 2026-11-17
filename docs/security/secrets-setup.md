# Sır Yönetimi ve Konfigürasyon Kurulumu

Bu belge, `appsettings.json` içinden çıkarılan sırların geliştirme ve dağıtım ortamlarında
nasıl sağlanacağını açıklar.

## 1. Neden

`appsettings.json` artık **hiçbir sır içermez**; yalnızca yapıyı ve boş değerleri tanımlar:

```json
"ConnectionStrings": { "DefaultConnection": "" },
"JwtSettings": { "Secret": "", ... }
```

Uygulama, bu değerler eksik veya zayıf olduğunda **başlamaz** (fail-fast):

| Kontrol | Kural | Hata |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Boş olamaz | `InvalidOperationException` |
| `JwtSettings:Secret` | En az 32 byte UTF-8 | `OptionsValidationException` |
| `JwtSettings:Issuer` | Mutlak URI olmalı | `OptionsValidationException` |
| `JwtSettings:Audience` | Boş olamaz | `OptionsValidationException` |
| `AllowedHosts` (Development dışı) | `*` olamaz | `InvalidOperationException` |

## 2. Geliştirme (`dotnet run`)

Sırlar **user secrets** deposunda tutulur (`AppointMan.Api.csproj` içinde `UserSecretsId` tanımlı).

```powershell
# JWT imza anahtarı uret
$secret = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))

# Anahtari ve baglanti dizesini secret store'a yaz
dotnet user-secrets set "JwtSettings:Secret" $secret --project .\AppointMan.Api\AppointMan.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=AppointManDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project .\AppointMan.Api\AppointMan.Api.csproj

# Kontrol
dotnet user-secrets list --project .\AppointMan.Api\AppointMan.Api.csproj
```

## 3. Docker Compose

`docker-compose.yml`, sırları `.env` dosyasından okur:

```powershell
Copy-Item .env.example .env
# .env icindeki placeholder'lari doldurun
docker compose up --build
```

`.env` **asla commit edilmez** (`.gitignore` içinde).

## 4. Production

Sırlar environment variable veya bir secret store (Azure Key Vault, AWS Secrets Manager,
Docker secret) üzerinden sağlanır. Çift alt çizgi (`__`) bölüm ayırıcıdır:

```text
ConnectionStrings__DefaultConnection=Server=...;User Id=...;Password=...;Encrypt=True
JwtSettings__Secret=<secret-store-reference>
JwtSettings__RequireHttpsMetadata=true
AllowedHosts=api.example.com;www.example.com
```

`AllowedHosts` production'da somut host listesi olmalıdır; aksi halde uygulama başlamaz.

## 5. Acil Yapılması Gerekenler (manuel)

Bu adım otomatikleştirilemeyen, operatör müdahalesi gerektiren işlerdir:

1. **Eski JWT anahtarı yanmış sayılır.** Depoya commit edilmiş olan
   `AppointMan-Super-Secret-Key-Minimum-32-Characters-Long!` değeri git geçmişinde kalıcıdır.
   Tüm ortamlarda **yeni anahtara** geçilmelidir (rotasyon). Rotasyon sonrası tüm mevcut
   access/refresh token'lar geçersiz olur; kullanıcılar yeniden giriş yapar.
2. **`SA_PASSWORD` değeri git geçmişinde bulunmaktadır.** SQL Server parolası değiştirilmeli
   ve git geçmişi için `gitleaks` / `trufflehog` ile tarama yapılmalıdır.
3. **`.env` dosyası takipten çıkarılmalı** (git geçmişinde kaldığı için):
   ```powershell
   git rm --cached .env
   ```
4. Production `appsettings.json` içinde `TrustServerCertificate=True` bulunmamalıdır.

## 6. Doğrulama

```powershell
# Sirlar temizlenmis mi?
Select-String -Path .\AppointMan.Api\appsettings.json -Pattern 'Secret|Password|ConnectionString'

# Uygulama eksik sir ile baslamiyor mu?
dotnet run --project .\AppointMan.Api\AppointMan.Api.csproj
# Beklenen: OptionsValidationException / InvalidOperationException ve cikis
```

## 7. Graylog Loglama

API, loglari **GELF formati uzerinden UDP** ile Graylog'a gonderir. Konsol loglari da korunur;
Graylog erisilemezse istek akisi **etkilenmez**, en fazla bir kez `stderr`'e uyari yazilir.

| Degisken | Aciklama | Varsayilan |
|---|---|---|
| `GRAYLOG_ENABLED` | `false` ise provider hic kaydedilmez (no-op) | `false` |
| `GRAYLOG_HOST` | Graylog GELF UDP adresi | `localhost` |
| `GRAYLOG_PORT` | GELF UDP portu | `12201` |
| `GRAYLOG_SOURCE` | GELF `host`/`source` alani | `AppointMan.Api` |
| `GRAYLOG_ENVIRONMENT` | `_environment` alani | bos |
| `GRAYLOG_MASKED_FIELDS` | Ek maskelenecek alanlar (virgul/noktali virgul ile) | bos |

Environment degiskenleri `appsettings.json` icindeki `Graylog` bolumune **tercih edilir**.

```powershell
$env:GRAYLOG_ENABLED = "true"
$env:GRAYLOG_HOST    = "graylog.internal"
$env:GRAYLOG_PORT    = "12201"
```

Varsayilan olarak maskelenen alanlar: `password`, `passwordHash`, `currentPassword`, `newPassword`,
`token`, `accessToken`, `refreshToken`, `refreshTokenHash`, `authorization`, `secret`, `apiKey`,
`apiKeyHash`, `creditCard`, `cardNumber`, `cvv`, `cvc`, `tckn`, `tcKimlikNo`, `iban`.

> **Not:** Yalnizca `application/json` govdeler loglanir. Form-encoded govdeler (orn. login)
> yapisal olarak maskelenemedigi icin icerigi hic yazilmaz, `<body_not_logged: ...>` olarak ozetlenir.

## 8. Grafana

Metrik panosunun admin parolasi `.env` uzerinden gelir ve `docker-compose.yml` icinde **hardcode edilmez**.

| Degisken | Aciklama |
|---|---|
| `GRAFANA_ADMIN_PASSWORD` | Grafana `admin` kullanicisinin parolasi |

```powershell
# .env icine guclu bir parola yazin (sablon: .env.example)
GRAFANA_ADMIN_PASSWORD=<guclu-bir-parola>
```

Grafana'da anonim erisim ve kullanici kaydi kapalidir (`GF_AUTH_ANONYMOUS_ENABLED=false`,
`GF_USERS_ALLOW_SIGN_UP=false`). Canli ortamda bu parolayi bir secret store'dan besleyin.

Metrik altyapisinin tamami icin bkz. `docs/observability/metrics.md`.

## 9. E-posta dogrulama (SMTP)

Kayit sonrasi gonderilen dogrulama e-postasi SMTP uzerinden yollanir. Ayarlar
`appsettings.json` icindeki `Email` bolumunden okunur ve ortam degiskenleri bu
degerleri **ezer**.

| Degisken | Aciklama |
|---|---|
| `EMAIL_HOST` | SMTP sunucu adresi. **Bos ise uygulama log moduna duser.** |
| `EMAIL_PORT` | SMTP portu (varsayilan `587`) |
| `EMAIL_USE_SSL` | `true` / `false` (varsayilan `true`) |
| `EMAIL_USERNAME` | SMTP kullanici adi (bos ise anonim gonderim denenir) |
| `EMAIL_PASSWORD` | SMTP sifresi — **asla log'a yazilmaz** |
| `EMAIL_FROM_ADDRESS` | Gonderen adresi |
| `EMAIL_FROM_NAME` | Gonderen gorunen adi |
| `EMAIL_VERIFICATION_LINK_BASE` | Dogrulama ucunun tam adresi; token `?token=...` olarak eklenir |

```powershell
# .env icine (sablon: .env.example)
EMAIL_HOST=smtp.ornek.com
EMAIL_PORT=587
EMAIL_USE_SSL=true
EMAIL_USERNAME=no-reply@ornek.com
EMAIL_PASSWORD=<guclu-bir-parola>
EMAIL_VERIFICATION_LINK_BASE=https://apiapp.rezervise.com/api/Auth/verify-email
```

### Davranis

- **Development** ortaminda veya `EMAIL_HOST` bos oldugunda e-posta gonderilmez;
  icerik uygulama log'una yazilir. Development'ta govde (dogrulama bagi dahil)
  gorunur, diger ortamlarda token sizmamasi icin yalnizca alici ve konu yazilir.
- Gonderim **arka planda** yapilir: kayit istegi SMTP gecikmesini veya hatasini
  beklemez. Kuyruk sinirlidir; doldugunda mesaj reddedilir ve uyari loglanir.
- SMTP hatasi kayit akisini **bozmaz**; kullanici `resend-verification`
  ucuyla yeni bag istemelidir.

> **Uretim notu:** `AppointMan.Api/appsettings.json` git-ignored'dir. Canliya
> alirken `Email` bolumunun sunucudaki bu dosyaya veya ortam degiskenlerine
> eklendiginden emin olun; aksi halde dogrulama e-postalari gonderilmez.

