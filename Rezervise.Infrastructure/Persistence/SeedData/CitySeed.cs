using System.Globalization;
using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// İl (City) referans verilerini ekler.
    /// Id olarak Türk plaka kodları (1-81) kullanılır; böylece Address.CityId ile
    /// il/ilçe/mahalle zinciri aynı anahtar üzerinden birleşir.
    /// Kaynak: il.sql
    /// </summary>
    public static class CitySeed
    {
        /// <summary>Seed edilen tüm iller için sabit ülke kodu (ISO 3166-1 alpha-2).</summary>
        private const string CountryCode = "TR";

        /// <summary>Seed edilmesi beklenen il sayısı; döngü başlangıç kapasitesi için kullanılır.</summary>
        private const int ExpectedCityCount = 81;

        /// <summary>
        /// Plaka kodu ile il adını "|" ayırıcı ile tutan ham veri listesi.
        /// </summary>
        private static readonly string[] RawCities =
        {
            "1|Adana",
            "2|Adıyaman",
            "3|Afyonkarahisar",
            "4|Ağrı",
            "5|Amasya",
            "6|Ankara",
            "7|Antalya",
            "8|Artvin",
            "9|Aydın",
            "10|Balıkesir",
            "11|Bilecik",
            "12|Bingöl",
            "13|Bitlis",
            "14|Bölü",
            "15|Burdur",
            "16|Bursa",
            "17|Çanakkale",
            "18|Çankiri",
            "19|Çorum",
            "20|Denizli",
            "21|Diyarbakır",
            "22|Edirne",
            "23|Elazığ",
            "24|Erzincan",
            "25|Erzurum",
            "26|Eskişehir",
            "27|Gaziantep",
            "28|Giresun",
            "29|Gümüshane",
            "30|Hakkari",
            "31|Hatay",
            "32|Isparta",
            "33|Mersin(Içel)",
            "34|Istanbul",
            "35|İzmir",
            "36|Kars",
            "37|Kastamonu",
            "38|Kayseri",
            "39|Kırklareli",
            "40|Kırşehir",
            "41|Kocaeli",
            "42|Konya",
            "43|Kütahya",
            "44|Malatya",
            "45|Manisa",
            "46|Kahramanmaras",
            "47|Mardın",
            "48|Muğla",
            "49|Müs",
            "50|Nevşehir",
            "51|Niğde",
            "52|Ordu",
            "53|Rize",
            "54|Sakarya",
            "55|Samsun",
            "56|Siirt",
            "57|Sinop",
            "58|Sivas",
            "59|Tekirdağ",
            "60|Tokat",
            "61|Trabzon",
            "62|Tunceli",
            "63|Şanlıurfa",
            "64|Uşak",
            "65|Van",
            "66|Yozgat",
            "67|Zonguldak",
            "68|Aksaray",
            "69|Bayburt",
            "70|Karaman",
            "71|Kırıkkale",
            "72|Batman",
            "73|Şırnak",
            "74|Bartın",
            "75|Ardahan",
            "76|Iğdır",
            "77|Yalova",
            "78|Karabük",
            "79|Kilis",
            "80|Osmaniye",
            "81|Düzce"
        };

        /// <summary>
        /// İlleri veritabanına ekler. Tablo dolu ise hiçbir şey yapmaz.
        /// </summary>
        /// <param name="context">Kullanılacak veritabanı bağlamı.</param>
        /// <param name="logger">Seed ilerlemesini raporlamak için loglayıcı.</param>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Cities.AnyAsync())
            {
                logger.LogInformation("Cities zaten mevcut, atlanıyor.");
                return;
            }

            var cities = new List<City>(ExpectedCityCount);

            foreach (var line in RawCities)
            {
                // Ham satır formatı: <plakaKodu>|<ilAdi>
                var separatorIndex = line.IndexOf('|');
                var id = int.Parse(line[..separatorIndex], CultureInfo.InvariantCulture);
                var name = line[(separatorIndex + 1)..];

                cities.Add(new City(id, name, CountryCode));
            }

            await context.Cities.AddRangeAsync(cities);
            await context.SaveChangesAsync();

            logger.LogInformation("Cities seed edildi: {Count} il", cities.Count);
        }
    }
}
