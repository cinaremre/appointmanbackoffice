using System.Globalization;
using AppointMan.Domain.Entities;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Il (City) referans verilerini ekler.
    /// Id olarak Turk plaka kodlari (1-81) kullanilir; boylece Address.CityId ile
    /// il/ilce/mahalle zinciri ayni anahtar uzerinden birlesir.
    /// Kaynak: il.sql
    /// </summary>
    public static class CitySeed
    {
        /// <summary>Seed edilen tum iller icin sabit ulke kodu (ISO 3166-1 alpha-2).</summary>
        private const string CountryCode = "TR";

        /// <summary>Seed edilmesi beklenen il sayisi; dongu baslangic kapasitesi icin kullanilir.</summary>
        private const int ExpectedCityCount = 81;

        /// <summary>
        /// Plaka kodu ile il adini "|" ayirici ile tutan ham veri listesi.
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
            "14|Bolu",
            "15|Burdur",
            "16|Bursa",
            "17|Çanakkale",
            "18|Çankırı",
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
            "29|Gümüşhane",
            "30|Hakkari",
            "31|Hatay",
            "32|Isparta",
            "33|Mersin(İçel)",
            "34|İstanbul",
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
            "46|Kahramanmaraş",
            "47|Mardin",
            "48|Muğla",
            "49|Muş",
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
        /// Illeri veritabanina ekler. Tablo dolu ise hicbir sey yapmaz.
        /// </summary>
        /// <param name="context">Kullanilacak veritabani baglami.</param>
        /// <param name="logger">Seed ilerlemesini raporlamak icin loglayici.</param>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Cities.AnyAsync())
            {
                logger.LogInformation("Cities zaten mevcut, atlaniyor.");
                return;
            }

            var cities = new List<City>(ExpectedCityCount);

            foreach (var line in RawCities)
            {
                // Ham satir formati: <plakaKodu>|<ilAdi>
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
