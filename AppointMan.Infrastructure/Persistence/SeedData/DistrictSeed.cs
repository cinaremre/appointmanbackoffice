using System.Globalization;
using AppointMan.Domain.Entities;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Ilce (District) referans verilerini ekler.
    /// Kaynak: ilce.sql — 958 ilce, il ID'sine gore gruplanmis halde.
    /// Kaynak veride ilce ID'leri 1'den baslayarak sirali oldugu icin ID'ler
    /// bu siraya gore otomatik atanir; mahalle seed'i bu ID'lere baglanir.
    /// </summary>
    public static class DistrictSeed
    {
        /// <summary>Kaynak verideki toplam ilce sayisi; dogrulama ve kapasite icin kullanilir.</summary>
        private const int ExpectedDistrictCount = 958;

        /// <summary>
        /// Her elemani "&lt;ilId&gt;:ilce1,ilce2,..." formatinda olan ham veri listesi.
        /// Ilce adlari kaynak SQL'deki sirayi (yani ilce ID'sini) koruyacak sekilde
        /// sirali yazilmistir.
        /// </summary>
        private static readonly string[] RawDistricts =
        {
            "1:Aladağ(Karsantı),Ceyhan,Çukurova,Feke,İmamoğlu,Karaisalı,Karataş,Kozan,Pozantı,Saimbeyli,Sarıçam,Seyhan,Tufanbeyli,Yumurtalık,Yüreğir",
            "2:Adıyaman,Besni,Çelikhan,Gerger,Gölbaşı,Kahta,Samsat,Sincik,Tut",
            "3:Afyonkarahisar,Başmakçı,Bayat,Bolvadin,Çay,Çobanlar,Dazkırı,Dinar,Emirdağ,Evciler,Hocalar,İhsaniye,İscehisar,Kızılören,Sandıklı,Sincanlı(Sinanpaşa),Sultandağı,Şuhut",
            "4:Ağrı,Diyadin,Doğubeyazıt,Eleşkirt,Hamur,Patnos,Taşlıçay,Tutak",
            "5:Amasya,Göynücek,Gümüşhacıköy,Hamamözü,Merzifon,Suluova,Taşova",
            "6:Akyurt,Altındağ,Ayaş,Bala,Beypazarı,Çamlıdere,Çankaya,Çubuk,Elmadağ,Etimesgut,Evren,Gölbaşı,Güdül,Haymana,Kalecik,Kazan,Keçiören,Kızılcahamam,Mamak,Nallıhan,Polatlı,Pursaklar,Sincan,Şereflikoçhisar,Yenimahalle",
            "7:Akseki,Aksu,Alanya,Döşemealtı,Elmalı,Finike,Gazipaşa,Gündoğmuş,İbradı(Aydınkent),Kale(Demre),Kaş,Kemer,Kepez,Konyaaltı,Korkuteli,Kumluca,Manavgat,Muratpaşa,Serik",
            "8:Ardanuç,Arhavi,Artvin,Borçka,Hopa,Murgul(Göktaş),Şavşat,Yusufeli",
            "9:Aydın,Bozdoğan,Buharkent(Çubukdağı),Çine,Didim(Yenihisar),Germencik,İncirliova,Karacasu,Karpuzlu,Koçarlı,Köşk,Kuşadası,Kuyucak,Nazilli,Söke,Sultanhisar,Yenipazar",
            "10:Ayvalık,Balıkesir,Balya,Bandırma,Bigadiç,Burhaniye,Dursunbey,Edremit,Erdek,Gömeç,Gönen,Havran,İvrindi,Kepsut,Manyas,Marmara,Savaştepe,Sındırgı,Susurluk",
            "11:Bilecik,Bozüyük,Gölpazarı,İnhisar,Osmaneli,Pazaryeri,Söğüt,Yenipazar",
            "12:Adaklı,Bingöl,Genç,Karlıova,Kığı,Solhan,Yayladere,Yedisu",
            "13:Adilcevaz,Ahlat,Bitlis,Güroymak,Hizan,Mutki,Tatvan",
            "14:Bolu,Dörtdivan,Gerede,Göynük,Kıbrıscık,Mengen,Mudurnu,Seben,Yeniçağa",
            "15:Ağlasun,Altınyayla(Dirmil),Bucak,Burdur,Çavdır,Çeltikçi,Gölhisar,Karamanlı,Kemer,Tefenni,Yeşilova",
            "16:Büyükorhan,Gemlik,Gürsu,Harmancık,İnegöl,İznik,Karacabey,Keles,Kestel,Mudanya,Mustafakemalpaşa,Nilüfer,Orhaneli,Orhangazi,Osmangazi,Yenişehir,Yıldırım",
            "17:Ayvacık,Bayramiç,Biga,Bozcaada,Çan,Çanakkale,Eceabat,Ezine,Gelibolu,Gökçeada(İmroz),Lapseki,Yenice",
            "18:Atkaracalar,Bayramören,Çankırı,Çerkeş,Eldivan,Ilgaz,Kızılırmak,Korgun,Kurşunlu,Orta,Şabanözü,Yapraklı",
            "19:Alaca,Bayat,Boğazkale,Çorum,Dodurga,İskilip,Kargı,Laçin,Mecitözü,Oğuzlar(Karaören),Ortaköy,Osmancık,Sungurlu,Uğurludağ",
            "20:Acıpayam,Akköy,Babadağ,Baklan,Bekilli,Beyağaç,Bozkurt,Buldan,Çal,Çameli,Çardak,Çivril,Denizli,Güney,Honaz,Kale,Sarayköy,Serinhisar,Tavas",
            "21:Bağlar,Bismil,Çermik,Çınar,Çüngüş,Dicle,Eğil,Ergani,Hani,Hazro,Kayapınar,Kocaköy,Kulp,Lice,Silvan,Sur,Yenişehir",
            "22:Edirne,Enez,Havsa,İpsala,Keşan,Lalapaşa,Meriç,Süleoğlu(Süloğlu),Uzunköprü",
            "23:Ağın,Alacakaya,Arıcak,Baskil,Elazığ,Karakoçan,Keban,Kovancılar,Maden,Palu,Sivrice",
            "24:Çayırlı,Erzincan,İliç(Ilıç),Kemah,Kemaliye,Otlukbeli,Refahiye,Tercan,Üzümlü",
            "25:Aşkale,Aziziye(Ilıca),Çat,Hınıs,Horasan,İspir,Karaçoban,Karayazı,Köprüköy,Narman,Oltu,Olur,Palandöken,Pasinler,Pazaryolu,Şenkaya,Tekman,Tortum,Uzundere,Yakutiye",
            "26:Alpu,Beylikova,Çifteler,Günyüzü,Han,İnönü,Mahmudiye,Mihalgazi,Mihalıçcık,Odunpazarı,Sarıcakaya,Seyitgazi,Sivrihisar,Tepebaşı",
            "27:Araban,İslahiye,Karkamış,Nizip,Nurdağı,Oğuzeli,Şahinbey,Şehitkamil,Yavuzeli",
            "28:Alucra,Bulancak,Çamoluk,Çanakçı,Dereli,Doğankent,Espiye,Eynesil,Giresun,Görele,Güce,Keşap,Piraziz,Şebinkarahisar,Tirebolu,Yağlıdere",
            "29:Gümüşhane,Kelkit,Köse,Kürtün,Şiran,Torul",
            "30:Çukurca,Hakkari,Şemdinli,Yüksekova",
            "31:Altınözü,Antakya,Belen,Dörtyol,Erzin,Hassa,İskenderun,Kırıkhan,Kumlu,Reyhanlı,Samandağ,Yayladağı",
            "32:Aksu,Atabey,Eğridir(Eğirdir),Gelendost,Gönen,Isparta,Keçiborlu,Senirkent,Sütçüler,Şarkikaraağaç,Uluborlu,Yalvaç,Yenişarbademli",
            "33:Akdeniz,Anamur,Aydıncık,Bozyazı,Çamlıyayla,Erdemli,Gülnar (Gülpınar),Mezitli,Mut,Silifke,Tarsus,Toroslar,Yenişehir",
            "34:Adalar,Arnavutköy,Ataşehir,Avcılar,Bağcılar,Bahçelievler,Bakırköy,Başakşehir,Bayrampaşa,Beşiktaş,Beykoz,Beylikdüzü,Beyoğlu,Büyükçekmece,Çatalca,Çekmeköy,Esenler,Esenyurt,Eyüp,Fatih,Gaziosmanpaşa,Güngören,Kadıköy,Kağıthane,Kartal,Küçükçekmece,Maltepe,Pendik,Sancaktepe,Sarıyer,Silivri,Sultanbeyli,Sultangazi,Şile,Şişli,Tuzla,Ümraniye,Üsküdar,Zeytinburnu",
            "35:Aliağa,Balçova,Bayındır,Bayraklı,Bergama,Beydağ,Bornova,Buca,Cumaovası(Menderes),Çeşme,Çiğli,Dikili,Foça,Gaziemir,Güzelbahçe,Karabağlar,Karaburun,Karşıyaka,Kemalpaşa,Kınık,Kiraz,Konak,Menemen,Narlıdere,Ödemiş,Seferihisar,Selçuk,Tire,Torbalı,Urla",
            "36:Akyaka,Arpaçay,Digor,Kağızman,Kars,Sarıkamış,Selim,Susuz",
            "37:Abana,Ağlı,Araç,Azdavay,Bozkurt,Cide,Çatalzeytin,Daday,Devrekani,Doğanyurt,Hanönü(Gökçeağaç),İhsangazi,İnebolu,Kastamonu,Küre,Pınarbaşı,Seydiler,Şenpazar,Taşköprü,Tosya",
            "38:Akkışla,Bünyan,Develi,Felahiye,Hacılar,İncesu,Kocasinan,Melikgazi,Özvatan(Çukur),Pınarbaşı,Sarıoğlan,Sarız,Talas,Tomarza,Yahyalı,Yeşilhisar",
            "39:Babaeski,Demirköy,Kırklareli,Kofçaz,Lüleburgaz,Pehlivanköy,Pınarhisar,Vize",
            "40:Akçakent,Akpınar,Boztepe,Çiçekdağı,Kaman,Kırşehir,Mucur",
            "41:Başiskele,Çayırova,Darıca,Derince,Dilovası,Gebze,Gölcük,İzmit,Kandıra,Karamürsel,Kartepe,Tütünçiftlik",
            "42:Ahırlı,Akören,Akşehir,Altınekin,Beyşehir,Bozkır,Cihanbeyli,Çeltik,Çumra,Derbent,Derebucak,Doğanhisar,Emirgazi,Ereğli,Güneysınır,Hadim,Halkapınar,Hüyük,Ilgın,Kadınhanı,Karapınar,Karatay,Kulu,Meram,Sarayönü,Selçuklu,Seydişehir,Taşkent,Tuzlukçu,Yalıhüyük,Yunak",
            "43:Altıntaş,Aslanapa,Çavdarhisar,Domaniç,Dumlupınar,Emet,Gediz,Hisarcık,Kütahya,Pazarlar,Simav,Şaphane,Tavşanlı,Tunçbilek",
            "44:Akçadağ,Arapkir,Arguvan,Battalgazi,Darende,Doğanşehir,Doğanyol,Hekimhan,Kale,Kuluncak,Malatya,Pötürge,Yazıhan,Yeşilyurt",
            "45:Ahmetli,Akhisar,Alaşehir,Demirci,Gölmarmara,Gördes,Kırkağaç,Köprübaşı,Kula,Manisa,Salihli,Sarıgöl,Saruhanlı,Selendi,Soma,Turgutlu",
            "46:Afşin,Andırın,Çağlayancerit,Ekinözü,Elbistan,Göksun,Kahramanmaraş,Nurhak,Pazarcık,Türkoğlu",
            "47:Dargeçit,Derik,Kızıltepe,Mardin,Mazıdağı,Midyat(Estel),Nusaybin,Ömerli,Savur,Yeşilli",
            "48:Bodrum,Dalaman,Datça,Fethiye,Kavaklıdere,Köyceğiz,Marmaris,Milas,Muğla,Ortaca,Ula,Yatağan",
            "49:Bulanık,Hasköy,Korkut,Malazgirt,Muş,Varto",
            "50:Acıgöl,Avanos,Derinkuyu,Gülşehir,Hacıbektaş,Kozaklı,Nevşehir,Ürgüp",
            "51:Altunhisar,Bor,Çamardı,Çiftlik(Özyurt),Niğde,Ulukışla",
            "52:Akkuş,Aybastı,Çamaş,Çatalpınar,Çaybaşı,Fatsa,Gölköy,Gülyalı,Gürgentepe,İkizce,Kabataş,Karadüz(Kabadüz),Korgan,Kumru,Mesudiye,Ordu,Perşembe,Ulubey,Ünye",
            "53:Ardeşen,Çamlıhemşin,Çayeli,Derepazarı,Fındıklı,Güneysu,Hemşin,İkizdere,İyidere,Kalkandere,Pazar,Rize",
            "54:Adapazarı,Akyazı,Arifiye,Erenler,Ferizli,Geyve,Hendek,Karapürçek,Karasu,Kaynarca,Kocaali,Pamukova,Sapanca,Serdivan,Söğütlü,Taraklı",
            "55:19 Mayıs(Ballıca),Alaçam,Asarcık,Atakum,Ayvacık,Bafra,Canik,Çarşamba,Havza,İlkadım,Kavak,Ladik,Salıpazarı,Tekkeköy,Terme,Vezirköprü,Yakakent",
            "56:Aydınlar,Baykan,Eruh,Kurtalan,Pervari,Siirt,Şirvan",
            "57:Ayancık,Boyabat,Dikmen,Durağan,Erfelek,Gerze,Saraydüzü,Sinop,Türkeli",
            "58:Akıncılar,Altınyayla,Divriği,Doğanşar,Gemerek,Gölova,Gürün,Hafik,İmranlı,Kangal,Koyulhisar,Sivas,Suşehri,Şarkışla,Ulaş,Yıldızeli,Zara",
            "59:Çerkezköy,Çorlu,Hayrabolu,Malkara,Marmaraereğlisi,Muratlı,Saray,Şarköy,Tekirdağ",
            "60:Almus,Artova,Başçiftlik,Erbaa,Niksar,Pazar,Reşadiye,Sulusaray,Tokat,Turhal,Yeşilyurt,Zile",
            "61:Akçaabat,Araklı,Arsin,Beşikdüzü,Çarşıbaşı,Çaykara,Dernekpazarı,Düzköy,Hayrat,Köprübaşı,Maçka,Of,Sürmene,Şalpazarı,Tonya,Trabzon,Vakfıkebir,Yomra",
            "62:Çemişgezek,Hozat,Mazgirt,Nazımiye,Ovacık,Pertek,Pülümür,Tunceli",
            "63:Akçakale,Birecik,Bozova,Ceylanpınar,Halfeti,Harran,Hilvan,Siverek,Suruç,Şanlıurfa,Viranşehir",
            "64:Banaz,Eşme,Karahallı,Sivaslı,Ulubey,Uşak",
            "65:Bahçesaray,Başkale,Çaldıran,Çatak,Edremit(Gümüşdere),Erciş,Gevaş,Gürpınar,Muradiye,Özalp,Saray,Van",
            "66:Akdağmadeni,Aydıncık,Boğazlıyan,Çandır,Çayıralan,Çekerek,Kadışehri,Saraykent,Sarıkaya,Sorgun,Şefaatli,Yenifakılı,Yerköy,Yozgat",
            "67:Alaplı,Çaycuma,Devrek,Gökçebey,Karadenizereğli,Zonguldak",
            "68:Ağaçören,Aksaray,Eskil,Gülağaç(Ağaçlı),Güzelyurt,Ortaköy,Sarıyahşi",
            "69:Aydıntepe,Bayburt,Demirözü",
            "70:Ayrancı,Başyayla,Ermenek,Karaman,Kazımkarabekir,Sarıveliler",
            "71:Bahşili,Balışeyh,Çelebi,Delice,Karakeçili,Keskin,Kırıkkale,Sulakyurt,Yahşihan",
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
        /// Ilceleri veritabanina ekler. Tablo dolu ise hicbir sey yapmaz.
        /// Ilce ID'si kaynak verideki siraya gore 1'den baslayarak atanir.
        /// </summary>
        /// <param name="context">Kullanilacak veritabani baglami.</param>
        /// <param name="logger">Seed ilerlemesini raporlamak icin loglayici.</param>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.Districts.AnyAsync())
            {
                logger.LogInformation("Districts zaten mevcut, atlaniyor.");
                return;
            }

            var districts = new List<District>(ExpectedDistrictCount);
            var districtId = 1;

            foreach (var line in RawDistricts)
            {
                // Ham satir formati: <ilId>:ilce1,ilce2,...
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
                // Kaynak veri beklenenden farkli sayida ilce iceriyorsa ID kaymasi
                // olusacagi icin seed'i baslatmadan durduruyoruz.
                logger.LogWarning(
                    "Districts seed iptal edildi: beklenen {Expected}, bulunan {Actual}",
                    ExpectedDistrictCount, districts.Count);
                return;
            }

            await context.Districts.AddRangeAsync(districts);
            await context.SaveChangesAsync();

            logger.LogInformation("Districts seed edildi: {Count} ilce", districts.Count);
        }
    }
}
