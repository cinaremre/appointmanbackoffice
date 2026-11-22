# Rezervise Backoffice (React)

Platform yönetim paneli frontend'i. Ayrı bir npm projesidir; .NET solution'ına
dahil değildir. Backend: `../Rezervise.BackOffice.Api` (bkz. ana repo `docs/workflows.md`).

## Stack

- React 19 + TypeScript + Vite
- MUI (kurumsal arayüz), TanStack Query (server state)
- React Router (lazy route'lar), React Hook Form + Zod (formlar)
- Vitest + React Testing Library

## Çalıştırma

```bash
npm install

# 1) Backend'i ayaga kaldirin (varsayilan http://localhost:5099):
#   ASPNETCORE_ENVIRONMENT=Development \
#   JwtSettings__Secret=<32+ karakter> \
#   ConnectionStrings__DefaultConnection=<sql baglantisi> \
#   dotnet run --project ../Rezervise.BackOffice.Api --urls http://localhost:5099

# 2) Frontend:
npm run dev      # http://localhost:5173 — /api istekleri 5099'a proxy'lenir
```

Giriş: backoffice rollerinden birine sahip kullanıcı (ör. seed: `admin@example.com`).

## Mimari

Feature-based modüler yapı (doküman 4.1): `src/app` (router/guard/layout),
`src/shared` (tek çekirdekli tipli API client, DataTable, durum bileşenleri,
permission matrisi), `src/features/<modül>` (sayfalar).

- **Oturum**: token sessionStorage'da (localStorage kullanılmaz); sekme kapanınca biter.
- **Yetki (UX)**: rol→permission matrisi menü/butonları gizler; nihai kontrol backend policy'lerindedir.
- **Hatalar**: RFC 9457 ProblemDetails merkezî `ApiError`'a çözülür; destek için traceId gösterilir.
- **Kritik işlemler** (askıya alma, kilitleme, arşivleme, iptal): gerekçe zorunlu onay diyalogu.

## Test

```bash
npm test              # vitest (birim + bileşen)
npm run build         # tsc + vite üretim derlemesi
```

## API tiplerini üretme (DTO senkronizasyonu)

Backend DTO alan adları frontend ile elle tutturulmaz. BackOffice API ayakta iken
Swagger'dan TypeScript tiplerini üretin:

```bash
# BackOffice.Api'yi calistirin (Development, ornek: http://localhost:5099)
npm run generate:types -- http://localhost:5099/swagger/v1/swagger.json
```

Üretilen `src/shared/api/generated/schema.d.ts` commit edilir; backend DTO
değişince script yeniden çalıştırılır. (Araç: `openapi-typescript`, geçici `npx` ile.)

