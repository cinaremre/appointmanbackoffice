# Migration model tutarliligi denetimi (Windows).
# CI veya commit oncesi calistirin; bekleyen model degisikligi varsa hata koduyla cikar.
# Gereksinim: dotnet-ef global arac.

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

Write-Host "== EF model migration uyumluluk denetimi =="
dotnet ef migrations has-pending-model-changes `
  --project Rezervise.Infrastructure `
  --startup-project Rezervise.Api.Mobil

if ($LASTEXITCODE -ne 0) {
  Write-Host "HATA: Bekleyen model degisikligi var. Yeni migration ekleyin veya snapshot'i duzeltin."
  exit 1
}

Write-Host "OK: Model ve migration uyumlu."
