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
