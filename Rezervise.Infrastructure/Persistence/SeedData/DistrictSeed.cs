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
            "1:Aladağ(Karsanti),Ceyhan,Çukurova,Feke,İmamoğlu,Karaisalı,Karataş,Kozan,Pozantı,Saimbeyli,Sariçam,Şeyhan,Tufanbeyli,Yumurtalık,Yüregir",
            "2:Adıyaman,Beşni,Çelikhan,Gerger,Gölbasi,Kahta,Samsat,Şincik,Tut",
            "3:Afyonkarahisar,Basmakçi,Bayat,Bolvadin,Çay,Çobanlar,Dazkiri,Dinar,Emirdağ,Evciler,Hocalar,İhsaniye,İşcehisar,Kizilören,Sandıklı,Sincanlı(Sinanpasa),Sultandağı,Şuhut",
            "4:Ağrı,Diyadin,Doğubeyazıt,Eleşkirt,Hamur,Patnos,Tasliçay,Tutak",
            "5:Amasya,Göynücek,Gümüshaciköy,Hamamözü,Merzifon,Suluova,Taşova",
            "6:Akyurt,Altındağ,Ayas,Bala,Beypazarı,Çamlidere,Çankaya,Çubuk,Elmadağ,Etimesgut,Evren,Gölbasi,Güdül,Haymana,Kalecik,Kazan,Keçiören,Kızılcahamam,Mamak,Nallihan,Polatlı,Pursaklar,Sincan,Sereflikoçhisar,Yenimahalle",
            "7:Akseki,Aksu,Alanya,Dösemealti,Elmalı,Finike,Gazipaşa,Gündogmus,İbradı(Aydınkent),Kale(Demre),Kas,Kemer,Kepez,Konyaaltı,Korküteli,Kumluca,Manavgat,Muratpasa,Serik",
            "8:Ardanuç,Arhavi,Artvin,Borçka,Hopa,Murgul(Göktas),Savsat,Yusufeli",
            "9:Aydın,Bozdoğan,Buharkent(Çubukdagi),Çine,Didim(Yenihisar),Germencik,İncirliova,Karacasu,Karpuzlu,Koçarli,Kösk,Kuşadası,Kuyucak,Nazilli,Söke,Sultanhisar,Yenipazar",
            "10:Ayvalık,Balıkesir,Balya,Bandırma,Bigadiç,Burhaniye,Dursunbey,Edremit,Erdek,Gömeç,Gönen,Havran,İvrindi,Kepsut,Manyas,Marmara,Savaştepe,Sindirgi,Susurluk",
            "11:Bilecik,Bozüyük,Gölpazari,İnhisar,Osmaneli,Pazaryeri,Sögüt,Yenipazar",
            "12:Adaklı,Bingöl,Genç,Karlıova,Kiği,Solhan,Yayladere,Yedişu",
            "13:Adilcevaz,Ahlat,Bitlis,Güroymak,Hizan,Mutki,Tatvan",
            "14:Bölü,Dörtdivan,Gerede,Göynük,Kıbrışçık,Mengen,Mudurnu,Seben,Yeniçaga",
            "15:Ağlasun,Altınyayla(Dirmil),Bucak,Burdur,Çavdir,Çeltikçi,Gölhisar,Karamanlı,Kemer,Tefenni,Yeşilova",
            "16:Büyükorhan,Gemlik,Gürsu,Harmancık,Inegöl,İznik,Karacabey,Keleş,Kestel,Mudanya,Mustafakemalpasa,Nilüfer,Orhaneli,Orhangazi,Osmangazi,Yenişehir,Yıldırım",
            "17:Ayvacık,Bayramiç,Biga,Bozcaada,Çan,Çanakkale,Eceabat,Ezine,Gelibolu,Gökçeada(İmroz),Lapşeki,Yenice",
            "18:Atkaracalar,Bayramören,Çankiri,Çerkes,Eldivan,Ilgaz,Kızılırmak,Korgun,Kurşunlu,Orta,Sabanözü,Yapraklı",
            "19:Alaca,Bayat,Boğazkale,Çorum,Dodurga,İskilip,Kargi,Laçin,Mecitözü,Oğuzlar(Karaören),Ortaköy,Osmancık,Sungurlu,Uğurludağ",
            "20:Acipayam,Akköy,Babadağ,Baklan,Bekilli,Beyagaç,Bozkurt,Buldan,Çal,Çameli,Çardak,Çivril,Denizli,Güney,Honaz,Kale,Sarayköy,Serinhisar,Tavas",
            "21:Bağlar,Bismil,Çermik,Çinar,Çüngüs,Dicle,Eğil,Ergani,Hanı,Hazro,Kayapınar,Kocaköy,Kulp,Lice,Silvan,Sür,Yenişehir",
            "22:Edirne,Enez,Havşa,İpsala,Keşan,Lalapasa,Meriç,Süleoglu(Süloglu),Uzunköprü",
            "23:Ağın,Alacakaya,Arıcak,Baskıl,Elazığ,Karakoçan,Keban,Kovancılar,Maden,Palu,Sivrice",
            "24:Çayirli,Erzincan,Iliç(Iliç),Kemah,Kemaliye,Otlukbeli,Refahıye,Tercan,Üzümlü",
            "25:Aşkale,Aziziye(Ilica),Çat,Hinis,Horasan,İspir,Karaçoban,Karayazı,Köprüköy,Narman,Oltu,Olur,Palandöken,Pasinler,Pazaryolu,Şenkaya,Tekman,Tortum,Uzundere,Yakutiye",
            "26:Alpu,Beylikova,Çifteler,Günyüzü,Han,Inönü,Mahmudiye,Mihalgazı,Mihaliçcik,Ödünpazarı,Sarıcakaya,Seyitgazi,Sivrihisar,Tepebaşı",
            "27:Araban,Islahıye,Karkamış,Nizip,Nurdağı,Oğuzeli,Şahinbey,Şehitkamil,Yavuzeli",
            "28:Alucra,Bulancak,Çamoluk,Çanakçi,Dereli,Doğankent,Espiye,Eynesil,Giresun,Görele,Güce,Kesap,Piraziz,Sebinkarahisar,Tirebolu,Yağlidere",
            "29:Gümüshane,Kelkit,Köse,Kürtün,Sıran,Torul",
            "30:Çukurca,Hakkari,Şemdinli,Yüksekova",
            "31:Altinözü,Antakya,Belen,Dörtyol,Erzin,Hassa,İskenderun,Kırıkhan,Kumlu,Reyhanlı,Samandağ,Yayladağı",
            "32:Aksu,Atabey,Eğridir(Eğirdir),Gelendost,Gönen,Isparta,Keçiborlu,Senirkent,Sütçüler,Sarkikaraagaç,Uluborlu,Yalvaç,Yenisarbademli",
            "33:Akdeniz,Anamur,Aydıncık,Bozyazı,Çamliyayla,Erdemli,Gülnar (Gülpinar),Mezitli,Müt,Sılıfke,Tarsus,Toroslar,Yenişehir",
            "34:Adalar,Arnavutköy,Ataşehir,Avcılar,Bağcılar,Bahçelievler,Bakirköy,Başakşehir,Bayrampasa,Beşiktaş,Beykoz,Beylikdüzü,Beyoğlu,Büyükçekmece,Çatalca,Çekmeköy,Esenler,Esenyurt,Eyüp,Fatih,Gaziosmanpasa,Güngören,Kadiköy,Kağıthane,Kartal,Küçükçekmece,Maltepe,Pendik,Sancaktepe,Sarıyer,Silivri,Sultanbeyli,Sultangazi,Sile,Şişli,Tuzla,Ümraniye,Üsküdar,Zeytinburnu",
            "35:Aliağa,Balçova,Bayındır,Bayraklı,Bergama,Beydağ,Bornova,Buca,Cumaovası(Menderes),Çesme,Çigli,Dikili,Foça,Gaziemir,Güzelbahçe,Karabağlar,Karaburun,Karşıyaka,Kemalpasa,Kinik,Kiraz,Konak,Menemen,Narlıdere,Ödemis,Seferihisar,Selçuk,Tire,Torbalı,Urla",
            "36:Akyaka,Arpaçay,Digor,Kağızman,Kars,Sarıkamış,Selim,Susuz",
            "37:Abana,Ağlı,Araç,Azdavay,Bozkurt,Cide,Çatalzeytin,Daday,Devrekanı,Doğanyurt,Hanönü(Gökçeagaç),İhsangazi,İnebolu,Kastamonu,Küre,Pınarbaşı,Şeydiler,Senpazar,Tasköprü,Tosya",
            "38:Akkışla,Bünyan,Develi,Felahiye,Hacılar,İncesu,Kocasınan,Melikgazi,Özvatan(Çukur),Pınarbaşı,Sarıoğlan,Sarız,Talaş,Tomarza,Yahyalı,Yeşilhisar",
            "39:Babaeski,Demirköy,Kırklareli,Kofçaz,Lüleburgaz,Pehlivanköy,Pınarhisar,Vize",
            "40:Akçakent,Akpınar,Boztepe,Çiçekdagi,Kaman,Kırşehir,Mucur",
            "41:Basiskele,Çayirova,Darıça,Derince,Dilovası,Gebze,Gölcük,İzmit,Kandıra,Karamürsel,Kartepe,Tütünçiftlik",
            "42:Ahirli,Akören,Akşehir,Altınekin,Beyşehir,Bozkır,Cihanbeyli,Çeltik,Çumra,Derbent,Derebucak,Doğanhisar,Emirgazi,Ereğli,Güneysinir,Hadim,Halkapınar,Hüyük,İlgin,Kadınhanı,Karapinar,Karatay,Kulü,Meram,Sarayönü,Selçuklu,Seydişehir,Taşkent,Tuzlukçu,Yalihüyük,Yunak",
            "43:Altıntas,Aslanapa,Çavdarhisar,Domaniç,Dumlupınar,Emet,Gediz,Hisarcık,Kütahya,Pazarlar,Simav,Saphane,Tavşanlı,Tunçbilek",
            "44:Akçadag,Arapkir,Arguvan,Battalgazı,Darende,Doğanşehir,Doğanyol,Hekimhan,Kale,Kuluncak,Malatya,Pötürge,Yazıhan,Yeşilyurt",
            "45:Ahmetli,Akhisar,Alaşehir,Demirci,Gölmarmara,Gördes,Kirkagaç,Köprübasi,Kula,Manisa,Salihli,Sarigöl,Saruhanlı,Selendi,Soma,Turgutlu",
            "46:Afşin,Andırın,Çaglayancerit,Ekinözü,Elbistan,Göksun,Kahramanmaras,Nurhak,Pazarcık,Türkoglu",
            "47:Dargeçit,Derik,Kızıltepe,Mardın,Mazıdağı,Midyat(Estel),Nusaybin,Ömerli,Savur,Yeşilli",
            "48:Bodrum,Dalaman,Datça,Fethiye,Kavaklıdere,Köycegiz,Marmaris,Milas,Muğla,Ortaca,Ula,Yatağan",
            "49:Bulanık,Hasköy,Korkut,Malazgirt,Müs,Varto",
            "50:Acigöl,Avanos,Derinkuyu,Gülsehir,Hacıbektaş,Kozaklı,Nevşehir,Ürgüp",
            "51:Altunhisar,Bor,Çamardi,Çiftlik(Özyurt),Niğde,Ulukısla",
            "52:Akkuş,Aybasti,Çamas,Çatalpinar,Çaybasi,Fatsa,Gölköy,Gülyali,Gürgentepe,İkizce,Kabatas,Karadüz(Kabadüz),Korgan,Kumru,Mesudiye,Ordu,Perşembe,Ulubey,Ünye",
            "53:Ardesen,Çamlihemsin,Çayeli,Derepazarı,Fındıklı,Güneysu,Hemşin,İkizdere,İyidere,Kalkandere,Pazar,Rize",
            "54:Adapazarı,Akyazı,Arifiye,Erenler,Ferizli,Geyve,Hendek,Karapürçek,Karasu,Kaynarça,Kocaali,Pamukova,Sapanca,Serdivan,Sögütlü,Taraklı",
            "55:19 Mayıs(Ballıca),Alaçam,Asarcık,Atakum,Ayvacık,Bafra,Canik,Çarsamba,Havza,İlkadim,Kavak,Ladik,Salipazarı,Tekkeköy,Terme,Vezirköprü,Yakakent",
            "56:Aydınlar,Baykan,Eruh,Kurtalan,Pervarı,Siirt,Şirvan",
            "57:Ayancık,Boyabat,Dikmen,Durağan,Erfelek,Gerze,Saraydüzü,Sinop,Türkeli",
            "58:Akıncılar,Altınyayla,Divriği,Doğansar,Gemerek,Gölova,Gürün,Hafik,İmranlı,Kangal,Koyulhisar,Sivas,Suşehri,Şarkışla,Ulaş,Yıldızeli,Zara",
            "59:Çerkezköy,Çorlu,Hayrabolu,Malkara,Marmaraereğlisi,Muratlı,Saray,Sarköy,Tekirdağ",
            "60:Almus,Artova,Basçiftlik,Erbaa,Niksar,Pazar,Reşadiye,Sulusaray,Tokat,Turhal,Yeşilyurt,Zile",
            "61:Akçaabat,Araklı,Arşın,Besikdüzü,Çarsibasi,Çaykara,Dernekpazarı,Düzköy,Hayrat,Köprübasi,Maçka,Of,Sürmene,Salpazarı,Tonya,Trabzon,Vakfıkebir,Yomra",
            "62:Çemisgezek,Hozat,Mazgırt,Nazımiye,Ovacık,Pertek,Pülümür,Tunceli",
            "63:Akçakale,Birecik,Bozova,Ceylanpınar,Halfeti,Harran,Hilvan,Siverek,Suruç,Şanlıurfa,Viranşehir",
            "64:Banaz,Esme,Karahalli,Sıvaslı,Ulubey,Uşak",
            "65:Bahçesaray,Başkale,Çaldiran,Çatak,Edremit(Gümüsdere),Ercis,Gevas,Gürpinar,Muradiye,Özalp,Saray,Van",
            "66:Akdağmadeni,Aydıncık,Boğazlıyan,Çandir,Çayiralan,Çekerek,Kadısehri,Saraykent,Sarıkaya,Sorgun,Sefaatlı,Yenifakılı,Yerköy,Yozgat",
            "67:Alaplı,Çaycuma,Devrek,Gökçebey,Karadenizereğli,Zonguldak",
            "68:Agaçören,Aksaray,Eskil,Gülagaç(Agaçli),Güzelyurt,Ortaköy,Sarıyahşi",
            "69:Aydıntepe,Bayburt,Demirözü",
            "70:Ayrancı,Başyayla,Ermenek,Karaman,Kazımkarabekir,Sarıveliler",
            "71:Bahsili,Baliseyh,Çelebi,Delice,Karakeçili,Keskin,Kırıkkale,Sulakyurt,Yahşıhan",
            "72:Batman,Beşiri,Gercüs,Hasankeyf,Kozluk,Sason",
            "73:Beytüssebap,Cizre,Güçlükonak,İdil,Silopi,Şırnak,Uludere",
            "74:Amasra,Bartın,Kurucasile,Ulus",
            "75:Ardahan,Çildir,Damal,Göle,Hanak,Posof",
            "76:Aralık,Iğdır,Karakoyunlu,Tuzluca",
            "77:Altınova,Armutlu,Çinarcik,Çiftlikköy,Termal,Yalova",
            "78:Eflanı,Eskıpazar,Karabük,Ovacık,Safranbolu,Yenice",
            "79:Elbeyli,Kilis,Müsabeyli,Polateli",
            "80:Bahçe,Düziçi,Hasanbeyli,Kadirli,Osmaniye,Sumbaş,Toprakkale",
            "81:Akçakoca,Cumayeri,Çilimli,Düzce,Gölyaka,Gümüsova,Kaynaşlı,Yığılca"
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
