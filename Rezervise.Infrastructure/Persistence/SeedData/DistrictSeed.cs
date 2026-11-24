using System.Globalization;
using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// İlçe (District) referans verilerini ekler.
    /// Kaynak: ilçe.sql — 958 ilçe, il ID'sine göre gruplanmış halde.
    /// Kaynak veride ilçe ID'leri 1'den başlayarak sıralı öldüğü için ID'ler
    /// bu sıraya göre otomatik atanır; mahalle seed'i bu ID'lere bağlanır.
    /// </summary>
    public static class DistrictSeed
    {
        /// <summary>Kaynak verideki toplam ilçe sayısı; doğrulama ve kapasite için kullanılır.</summary>
        private const int ExpectedDistrictCount = 958;

        /// <summary>
        /// Her elemanı "&lt;ilId&gt;:ilçe1,ilçe2,..." formatında olan ham veri listesi.
        /// İlçe adları kaynak SQL'deki sırayı (yanı ilçe ID'sını) koruyacak şekilde
        /// sıralı yazılmıştır.
        /// </summary>
        private static readonly string[] RawDistricts =
        {
            "1:Aladağ(Karsandı),Ceyhan,Çukurova,Feke,İmamoğlu,Karaisalı,Karataş,Kozan,Pozantı,Saimbeyli,Sarıçam,Seyhan,Tufanbeyli,Yumurtalık,Yüreğir",
            "2:Adıyaman,Besni,Çelikhan,Gerger,Gölbaşı,Kâhta,Samsat,Sincik,Tut",
            "3:Afyonkarahisar,Basmakçı,Bayat,Bolvadin,Çay,Çobanlar,Dazkırı,Dinar,Emirdağ,Evciler,Hocalar,İhsaniye,İscehisar,Kızılören,Sandıklı,Sinanpaşa(Sincanlı),Sultandağı,Şuhut",
            "4:Ağrı,Diyadin,Doğubayazıt,Eleşkirt,Hamur,Patnos,Taşlıçay,Tutak",
            "5:Amasya,Göynücek,Gümüşhacıköy,Hamamözü,Merzifon,Suluova,Taşova",
            "6:Akyurt,Altındağ,Ayaş,Bala,Beypazarı,Çamlıdere,Çankaya,Çubuk,Elmadağ,Etimesgut,Evren,Gölbaşı,Güdül,Haymana,Kalecik,Kazan,Keçiören,Kızılcahamam,Mamak,Nallıhan,Polatlı,Pursaklar,Sincan,Şereflikoçhisar,Yenimahalle",
            "7:Akseki,Aksu,Alanya,Döşemealtı,Elmalı,Finike,Gazipaşa,Gündoğmuş,İbradı(Aydınkent),Demre(Kale),Kaş,Kemer,Kepez,Konyaaltı,Korkuteli,Kumluca,Manavgat,Muratpaşa,Serik",
            "8:Ardanuç,Arhavi,Artvin,Borçka,Hopa,Murgul(Göktaş),Şavşat,Yusufeli",
            "9:Aydın,Bozdoğan,Buharkent(Çubukdağı),Çine,Didim(Yenihisar),Germencik,İncirliova,Karacasu,Karpuzlu,Koçarlı,Köşk,Kuşadası,Kuyucak,Nazilli,Söke,Sultanhisar,Yenipazar",
            "10:Ayvalık,Balıkesir,Balya,Bandırma,Bigadiç,Burhaniye,Dursunbey,Edremit,Erdek,Gömeç,Gönen,Havran,İvrindi,Kepsut,Manyas,Marmara,Savaştepe,Sındırgı,Susurluk",
            "11:Bilecik,Bozüyük,Gölpazarı,İnhisar,Osmaneli,Pazaryeri,Söğüt,Yenipazar",
            "12:Adaklı,Bingöl,Genç,Karlıova,Kiği,Solhan,Yayladere,Yedisu",
            "13:Adilcevaz,Ahlat,Bitlis,Güroymak,Hizan,Mutki,Tatvan",
            "14:Bolu,Dörtdivan,Gerede,Göynük,Kıbrıscık,Mengen,Mudurnu,Seben,Yeniçağa",
            "15:Ağlasun,Altınyayla(Dirmil),Bucak,Burdur,Çavdır,Çeltikçi,Gölhisar,Karamanlı,Kemer,Tefenni,Yeşilova",
            "16:Büyükorhan,Gemlik,Gürsu,Harmancık,İnegöl,İznik,Karacabey,Keles,Kestel,Mudanya,Mustafakemalpaşa,Nilüfer,Orhaneli,Orhangazi,Osmangazi,Yenişehir,Yıldırım",
            "17:Ayvacık,Bayramiç,Biga,Bozcaada,Çan,Çanakkale,Eceabat,Ezine,Gelibolu,Gökçeada(İmroz),Lapseki,Yenice",
            "18:Atkaracalar,Bayramören,Çankırı,Çerkeş,Eldivan,Ilgaz,Kızılırmak,Korgun,Kurşunlu,Orta,Şabanözü,Yapraklı",
            "19:Alaca,Bayat,Boğazkale,Çorum,Dodurga,İskilip,Kargı,Laçin,Mecitözü,Oğuzlar(Karaören),Ortaköy,Osmancık,Sungurlu,Uğurludağ",
            "20:Acıpayam,Akköy,Babadağ,Baklan,Bekilli,Beyağaç,Bozkurt,Buldan,Çal,Çameli,Çardak,Çivril,Denizli,Güney,Honaz,Kale,Sarayköy,Serinhisar,Tavas",
            "21:Bağlar,Bismil,Çermik,Çınar,Çüngüş,Dicle,Eğil,Ergani,Hani,Hazro,Kayapınar,Kocaköy,Kulp,Lice,Silvan,Sür,Yenişehir",
            "22:Edirne,Enez,Havşa,İpsala,Keşan,Lalapaşa,Meriç,Süloğlu,Uzunköprü",
            "23:Ağın,Alacakaya,Arıcak,Baskil,Elazığ,Karakoçan,Keban,Kovancılar,Maden,Palu,Sivrice",
            "24:Çayırlı,Erzincan,İliç,Kemah,Kemaliye,Otlukbeli,Refahıye,Tercan,Üzümlü",
            "25:Aşkale,Aziziye(Ilıca),Çat,Hınıs,Horasan,İspir,Karaçoban,Karayazı,Köprüköy,Narman,Oltu,Olur,Palandöken,Pasinler,Pazaryolu,Şenkaya,Tekman,Tortum,Uzundere,Yakutiye",
            "26:Alpu,Beylikova,Çifteler,Günyüzü,Han,İnönü,Mahmudiye,Mihalgazı,Mihalıçcık,Odunpazarı,Sarıcakaya,Seyitgazi,Sivrihisar,Tepebaşı",
            "27:Araban,İslahiye,Karkamış,Nizip,Nurdağı,Oğuzeli,Şahinbey,Şehitkamil,Yavuzeli",
            "28:Alucra,Bulancak,Çamoluk,Çanakçı,Dereli,Doğankent,Espiye,Eynesil,Giresun,Görele,Güce,Kesap,Piraziz,Şebinkarahisar,Tirebolu,Yağlıdere",
            "29:Gümüşhane,Kelkit,Köse,Kürtün,Şiran,Torul",
            "30:Çukurca,Hakkâri,Şemdinli,Yüksekova",
            "31:Altınözü,Antakya,Belen,Dörtyol,Erzin,Hassa,İskenderun,Kırıkhan,Kumlu,Reyhanlı,Samandağ,Yayladağı",
            "32:Aksu,Atabey,Eğirdir(Eğridir),Gelendost,Gönen,Isparta,Keçiborlu,Senirkent,Sütçüler,Şarkikaraağaç,Uluborlu,Yalvaç,Yenişarbademli",
            "33:Akdeniz,Anamur,Aydıncık,Bozyazı,Çamlıyayla,Erdemli,Gülnar(Gülpınar),Mezitli,Mut,Silifke,Tarsus,Toroslar,Yenişehir",
            "34:Adalar,Arnavutköy,Ataşehir,Avcılar,Bağcılar,Bahçelievler,Bakırköy,Başakşehir,Bayrampaşa,Beşiktaş,Beykoz,Beylikdüzü,Beyoğlu,Büyükçekmece,Çatalca,Çekmeköy,Esenler,Esenyurt,Eyüp,Fatih,Gaziosmanpaşa,Güngören,Kadıköy,Kağıthane,Kartal,Küçükçekmece,Maltepe,Pendik,Sancaktepe,Sarıyer,Silivri,Sultanbeyli,Sultangazi,Şile,Şişli,Tuzla,Ümraniye,Üsküdar,Zeytinburnu",
            "35:Aliağa,Balçova,Bayındır,Bayraklı,Bergama,Beydağ,Bornova,Buca,Menderes(Cumaovası),Çeşme,Çiğli,Dikili,Foça,Gaziemir,Güzelbahçe,Karabağlar,Karaburun,Karşıyaka,Kemalpaşa,Kınık,Kiraz,Konak,Menemen,Narlıdere,Ödemiş,Seferihisar,Selçuk,Tire,Torbalı,Urla",
            "36:Akyaka,Arpaçay,Digor,Kağızman,Kars,Sarıkamış,Selim,Susuz",
            "37:Abana,Ağlı,Araç,Azdavay,Bozkurt,Cide,Çatalzeytin,Daday,Devrekani,Doğanyurt,Hanönü(Gökçeağaç),İhsangazi,İnebolu,Kastamonu,Küre,Pınarbaşı,Şeydiler,Şenpazar,Taşköprü,Tosya",
            "38:Akkışla,Bünyan,Develi,Felahiye,Hacılar,İncesu,Kocasinan,Melikgazi,Özvatan(Çukur),Pınarbaşı,Sarıoğlan,Sarız,Talaş,Tomarza,Yahyalı,Yeşilhisar",
            "39:Babaeski,Demirköy,Kırklareli,Kofçaz,Lüleburgaz,Pehlivanköy,Pınarhisar,Vize",
            "40:Akçakent,Akpınar,Boztepe,Çiçekdağı,Kaman,Kırşehir,Mucur",
            "41:Başiskele,Çayırova,Darıca,Derince,Dilovası,Gebze,Gölcük,İzmit,Kandıra,Karamürsel,Kartepe,Tütünçiftlik",
            "42:Ahırlı,Akören,Akşehir,Altınekin,Beyşehir,Bozkır,Cihanbeyli,Çeltik,Çumra,Derbent,Derebucak,Doğanhisar,Emirgazi,Ereğli,Güneysınır,Hadim,Halkapınar,Hüyük,İlgün,Kadınhanı,Karapınar,Karatay,Kulu,Meram,Sarayönü,Selçuklu,Seydişehir,Taşkent,Tuzlukçu,Yalıhüyük,Yunak",
            "43:Altıntaş,Aslanapa,Çavdarhisar,Domaniç,Dumlupınar,Emet,Gediz,Hisarcık,Kütahya,Pazarlar,Simav,Şaphane,Tavşanlı,Tunçbilek",
            "44:Akçadağ,Arapgir,Arguvan,Battalgazı,Darende,Doğanşehir,Doğanyol,Hekimhan,Kale,Kuluncak,Malatya,Pötürge,Yazıhan,Yeşilyurt",
            "45:Ahmetli,Akhisar,Alaşehir,Demirci,Gölmarmara,Gördes,Kırkağaç,Köprübaşı,Kula,Manisa,Salihli,Sarıgöl,Saruhanlı,Selendi,Soma,Turgutlu",
            "46:Afşin,Andırın,Çağlayancerit,Ekinözü,Elbistan,Göksun,Kahramanmaraş,Nurhak,Pazarcık,Türkoğlu",
            "47:Dargeçit,Derik,Kızıltepe,Mardin,Mazıdağı,Midyat(Estel),Nusaybin,Ömerli,Savur,Yeşilli",
            "48:Bodrum,Dalaman,Datça,Fethiye,Kavaklıdere,Köyceğiz,Marmaris,Milas,Muğla,Ortaca,Ula,Yatağan",
            "49:Bulanık,Hasköy,Korkut,Malazgirt,Muş,Varto",
            "50:Acıgöl,Avanos,Derinkuyu,Gülşehir,Hacıbektaş,Kozaklı,Nevşehir,Ürgüp",
            "51:Altunhisar,Bor,Çamardı,Çiftlik(Özyurt),Niğde,Ulukışla",
            "52:Akkuş,Aybastı,Çamaş,Çatalpınar,Çaybaşı,Fatsa,Gölköy,Gülyalı,Gürgentepe,İkizce,Kabataş,Kabadüz(Karadüz),Korgan,Kumru,Mesudiye,Ordu,Perşembe,Ulubey,Ünye",
            "53:Ardeşen,Çamlıhemşin,Çayeli,Derepazarı,Fındıklı,Güneysu,Hemşin,İkizdere,İyidere,Kalkandere,Pazar,Rize",
            "54:Adapazarı,Akyazı,Arifiye,Erenler,Ferizli,Geyve,Hendek,Karapürçek,Karasu,Kaynarca,Kocaali,Pamukova,Sapanca,Serdivan,Söğütlü,Taraklı",
            "55:19 Mayıs(Ballıca),Alaçam,Asarcık,Atakum,Ayvacık,Bafra,Canik,Çarşamba,Havza,İlkadım,Kavak,Ladik,Salıpazarı,Tekkeköy,Terme,Vezirköprü,Yakakent",
            "56:Aydınlar,Baykan,Eruh,Kurtalan,Pervari,Siirt,Şirvan",
            "57:Ayancık,Boyabat,Dikmen,Durağan,Erfelek,Gerze,Saraydüzü,Sinop,Türkeli",
            "58:Akıncılar,Altınyayla,Divriği,Doğanşar,Gemerek,Gölova,Gürün,Hafik,İmranlı,Kangal,Koyulhisar,Sivas,Suşehri,Şarkışla,Ulaş,Yıldızeli,Zara",
            "59:Çerkezköy,Çorlu,Hayrabolu,Malkara,Marmaraereğlisi,Muratlı,Saray,Şarköy,Tekirdağ",
            "60:Almus,Artova,Başçiftlik,Erbaa,Niksar,Pazar,Reşadiye,Sulusaray,Tokat,Turhal,Yeşilyurt,Zile",
            "61:Akçaabat,Araklı,Arsin,Beşikdüzü,Çarşıbaşı,Çaykara,Dernekpazarı,Düzköy,Hayrat,Köprübaşı,Maçka,Of,Sürmene,Salpazarı,Tonya,Trabzon,Vakfıkebir,Yomra",
            "62:Çemişgezek,Hozat,Mazgırt,Nazımiye,Ovacık,Pertek,Pülümür,Tunceli",
            "63:Akçakale,Birecik,Bozova,Ceylanpınar,Halfeti,Harran,Hilvan,Siverek,Suruç,Şanlıurfa,Viranşehir",
            "64:Banaz,Eşme,Karahalli,Sivaslı,Ulubey,Uşak",
            "65:Bahçesaray,Başkale,Çaldıran,Çatak,Edremit(Gümüşdere),Erciş,Gevaş,Gürpınar,Muradiye,Özalp,Saray,Van",
            "66:Akdağmadeni,Aydıncık,Boğazlıyan,Çandır,Çayıralan,Çekerek,Kadışehri,Saraykent,Sarıkaya,Sorgun,Sefaatlı,Yenifakılı,Yerköy,Yozgat",
            "67:Alaplı,Çaycuma,Devrek,Gökçebey,Karadeniz Ereğli,Zonguldak",
            "68:Ağaçören,Aksaray,Eskil,Gülağaç(Ağaçlı),Güzelyurt,Ortaköy,Sarıyahşi",
            "69:Aydıntepe,Bayburt,Demirözü",
            "70:Ayrancı,Başyayla,Ermenek,Karaman,Kazımkarabekir,Sarıveliler",
            "71:Bahşılı,Balışeyh,Çelebi,Delice,Karakeçili,Keskin,Kırıkkale,Sulakyurt,Yahşıhan",
            "72:Batman,Beşiri,Gercüş,Hasankeyf,Kozluk,Sason",
            "73:Beytüşşebap,Cizre,Güçlükonak,İdil,Silopi,Şırnak,Uludere",
            "74:Amasra,Bartın,Kurucaşile,Ulus",
            "75:Ardahan,Çıldır,Damal,Göle,Hanak,Posof",
            "76:Aralık,Iğdır,Karakoyunlu,Tuzluca",
            "77:Altınova,Armutlu,Çınarcık,Çiftlikköy,Termal,Yalova",
            "78:Eflani,Eskipazar,Karabük,Ovacık,Safranbolu,Yenice",
            "79:Elbeyli,Kilis,Musabeyli,Polateli",
            "80:Bahçe,Düziçi,Hasanbeyli,Kadirli,Osmaniye,Sumbas,Toprakkale",
            "81:Akçakoca,Cumayeri,Çilimli,Düzce,Gölyaka,Gümüşova,Kaynaşlı,Yığılca"
        };

        /// <summary>
        /// İlçeleri veritabanına ekler. Tablo dolu ise hiçbir şey yapmaz.
        /// İlçe ID'si kaynak verideki sıraya göre 1'den başlayarak atanır.
        /// </summary>
        /// <param name="context">Kullanılacak veritabanı bağlamı.</param>
        /// <param name="logger">Seed ilerlemesini raporlamak için loglayıcı.</param>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Districts.AnyAsync())
            {
                logger.LogInformation("Districts zaten mevcut, atlanıyor.");
                return;
            }

            var districts = new List<District>(ExpectedDistrictCount);
            var districtId = 1;

            foreach (var line in RawDistricts)
            {
                // Ham satır formatı: <ilId>:ilçe1,ilçe2,...
                var separatorIndex = line.IndexOf(':');
                var cityId = int.Parse(line[..separatorIndex], CultureInfo.InvariantCulture);
                var names = line[(separatorIndex + 1)..].Split(',');

                foreach (var name in names)
                {
                    districts.Add(new District(districtId, cityId, name));
                    districtId++;
                }
            }

            if (districts.Count != ExpectedDistrictCount)
            {
                // Kaynak veri beklenenden farklı sayıda ilçe içeriyorsa ID kayması
                // oluşacağı için seed'i başlatmadan durduruyoruz.
                logger.LogWarning(
                    "Districts seed iptal edildi: beklenen {Expected}, bulunan {Actual}",
                    ExpectedDistrictCount, districts.Count);
                return;
            }

            await context.Districts.AddRangeAsync(districts);
            await context.SaveChangesAsync();

            logger.LogInformation("Districts seed edildi: {Count} ilçe", districts.Count);
        }
    }
}
