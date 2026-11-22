# Migration model tutarliligi denetimi.
# CI veya commit oncesi calistirin: EF modeli son migration ile uyumlu degilse
# (pending model changes) bu betik hata koduyla cikar. Boylece elle duzeltilen
# AppDbContextModelSnapshot gibi kopuslar bir daha gozden kacar.
#
# Gereksinim: dotnet-ef global arac.
#   dotnet tool install --global dotnet-ef

set -euo pipefail

cd "$(dirname "$0")/.."

echo "== EF model migration uyumluluk denetimi =="
dotnet ef migrations has-pending-model-changes \
  --project Rezervise.Infrastructure \
  --startup-project Rezervise.Api.Mobil

if [ $? -ne 0 ]; then
  echo "HATA: Bekleyen model degisikligi var. Yeni migration ekleyin veya snapshot'i duzeltin."
  exit 1
fi

echo "OK: Model ve migration uyumlu."
