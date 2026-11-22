using System.Globalization;
using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
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
            "1:Aladag(Karsanti),Ceyhan,Çukurova,Feke,Imamoglu,Karaisali,Karatas,Kozan,Pozanti,Saimbeyli,Sariçam,Seyhan,Tufanbeyli,Yumurtalik,Yüregir",
            "2:Adiyaman,Besni,Çelikhan,Gerger,Gölbasi,Kahta,Samsat,Sincik,Tut",
            "3:Afyonkarahisar,Basmakçi,Bayat,Bolvadin,Çay,Çobanlar,Dazkiri,Dinar,Emirdag,Evciler,Hocalar,Ihsaniye,Iscehisar,Kizilören,Sandikli,Sincanli(Sinanpasa),Sultandagi,Suhut",
            "4:Agri,Diyadin,Dogubeyazit,Eleskirt,Hamur,Patnos,Tasliçay,Tutak",
            "5:Amasya,Göynücek,Gümüshaciköy,Hamamözü,Merzifon,Suluova,Tasova",
            "6:Akyurt,Altindag,Ayas,Bala,Beypazari,Çamlidere,Çankaya,Çubuk,Elmadag,Etimesgut,Evren,Gölbasi,Güdül,Haymana,Kalecik,Kazan,Keçiören,Kizilcahamam,Mamak,Nallihan,Polatli,Pursaklar,Sincan,Sereflikoçhisar,Yenimahalle",
            "7:Akseki,Aksu,Alanya,Dösemealti,Elmali,Finike,Gazipasa,Gündogmus,Ibradi(Aydinkent),Kale(Demre),Kas,Kemer,Kepez,Konyaalti,Korkuteli,Kumluca,Manavgat,Muratpasa,Serik",
            "8:Ardanuç,Arhavi,Artvin,Borçka,Hopa,Murgul(Göktas),Savsat,Yusufeli",
            "9:Aydin,Bozdogan,Buharkent(Çubukdagi),Çine,Didim(Yenihisar),Germencik,Incirliova,Karacasu,Karpuzlu,Koçarli,Kösk,Kusadasi,Kuyucak,Nazilli,Söke,Sultanhisar,Yenipazar",
            "10:Ayvalik,Balikesir,Balya,Bandirma,Bigadiç,Burhaniye,Dursunbey,Edremit,Erdek,Gömeç,Gönen,Havran,Ivrindi,Kepsut,Manyas,Marmara,Savastepe,Sindirgi,Susurluk",
            "11:Bilecik,Bozüyük,Gölpazari,Inhisar,Osmaneli,Pazaryeri,Sögüt,Yenipazar",
            "12:Adakli,Bingöl,Genç,Karliova,Kigi,Solhan,Yayladere,Yedisu",
            "13:Adilcevaz,Ahlat,Bitlis,Güroymak,Hizan,Mutki,Tatvan",
            "14:Bolu,Dörtdivan,Gerede,Göynük,Kibriscik,Mengen,Mudurnu,Seben,Yeniçaga",
            "15:Aglasun,Altinyayla(Dirmil),Bucak,Burdur,Çavdir,Çeltikçi,Gölhisar,Karamanli,Kemer,Tefenni,Yesilova",
            "16:Büyükorhan,Gemlik,Gürsu,Harmancik,Inegöl,Iznik,Karacabey,Keles,Kestel,Mudanya,Mustafakemalpasa,Nilüfer,Orhaneli,Orhangazi,Osmangazi,Yenisehir,Yildirim",
            "17:Ayvacik,Bayramiç,Biga,Bozcaada,Çan,Çanakkale,Eceabat,Ezine,Gelibolu,Gökçeada(Imroz),Lapseki,Yenice",
            "18:Atkaracalar,Bayramören,Çankiri,Çerkes,Eldivan,Ilgaz,Kizilirmak,Korgun,Kursunlu,Orta,Sabanözü,Yaprakli",
            "19:Alaca,Bayat,Bogazkale,Çorum,Dodurga,Iskilip,Kargi,Laçin,Mecitözü,Oguzlar(Karaören),Ortaköy,Osmancik,Sungurlu,Ugurludag",
            "20:Acipayam,Akköy,Babadag,Baklan,Bekilli,Beyagaç,Bozkurt,Buldan,Çal,Çameli,Çardak,Çivril,Denizli,Güney,Honaz,Kale,Sarayköy,Serinhisar,Tavas",
            "21:Baglar,Bismil,Çermik,Çinar,Çüngüs,Dicle,Egil,Ergani,Hani,Hazro,Kayapinar,Kocaköy,Kulp,Lice,Silvan,Sur,Yenisehir",
            "22:Edirne,Enez,Havsa,Ipsala,Kesan,Lalapasa,Meriç,Süleoglu(Süloglu),Uzunköprü",
            "23:Agin,Alacakaya,Aricak,Baskil,Elazig,Karakoçan,Keban,Kovancilar,Maden,Palu,Sivrice",
            "24:Çayirli,Erzincan,Iliç(Iliç),Kemah,Kemaliye,Otlukbeli,Refahiye,Tercan,Üzümlü",
            "25:Askale,Aziziye(Ilica),Çat,Hinis,Horasan,Ispir,Karaçoban,Karayazi,Köprüköy,Narman,Oltu,Olur,Palandöken,Pasinler,Pazaryolu,Senkaya,Tekman,Tortum,Uzundere,Yakutiye",
            "26:Alpu,Beylikova,Çifteler,Günyüzü,Han,Inönü,Mahmudiye,Mihalgazi,Mihaliçcik,Odunpazari,Saricakaya,Seyitgazi,Sivrihisar,Tepebasi",
            "27:Araban,Islahiye,Karkamis,Nizip,Nurdagi,Oguzeli,Sahinbey,Sehitkamil,Yavuzeli",
            "28:Alucra,Bulancak,Çamoluk,Çanakçi,Dereli,Dogankent,Espiye,Eynesil,Giresun,Görele,Güce,Kesap,Piraziz,Sebinkarahisar,Tirebolu,Yaglidere",
            "29:Gümüshane,Kelkit,Köse,Kürtün,Siran,Torul",
            "30:Çukurca,Hakkari,Semdinli,Yüksekova",
            "31:Altinözü,Antakya,Belen,Dörtyol,Erzin,Hassa,Iskenderun,Kirikhan,Kumlu,Reyhanli,Samandag,Yayladagi",
            "32:Aksu,Atabey,Egridir(Egirdir),Gelendost,Gönen,Isparta,Keçiborlu,Senirkent,Sütçüler,Sarkikaraagaç,Uluborlu,Yalvaç,Yenisarbademli",
            "33:Akdeniz,Anamur,Aydincik,Bozyazi,Çamliyayla,Erdemli,Gülnar (Gülpinar),Mezitli,Mut,Silifke,Tarsus,Toroslar,Yenisehir",
            "34:Adalar,Arnavutköy,Atasehir,Avcilar,Bagcilar,Bahçelievler,Bakirköy,Basaksehir,Bayrampasa,Besiktas,Beykoz,Beylikdüzü,Beyoglu,Büyükçekmece,Çatalca,Çekmeköy,Esenler,Esenyurt,Eyüp,Fatih,Gaziosmanpasa,Güngören,Kadiköy,Kagithane,Kartal,Küçükçekmece,Maltepe,Pendik,Sancaktepe,Sariyer,Silivri,Sultanbeyli,Sultangazi,Sile,Sisli,Tuzla,Ümraniye,Üsküdar,Zeytinburnu",
            "35:Aliaga,Balçova,Bayindir,Bayrakli,Bergama,Beydag,Bornova,Buca,Cumaovasi(Menderes),Çesme,Çigli,Dikili,Foça,Gaziemir,Güzelbahçe,Karabaglar,Karaburun,Karsiyaka,Kemalpasa,Kinik,Kiraz,Konak,Menemen,Narlidere,Ödemis,Seferihisar,Selçuk,Tire,Torbali,Urla",
            "36:Akyaka,Arpaçay,Digor,Kagizman,Kars,Sarikamis,Selim,Susuz",
            "37:Abana,Agli,Araç,Azdavay,Bozkurt,Cide,Çatalzeytin,Daday,Devrekani,Doganyurt,Hanönü(Gökçeagaç),Ihsangazi,Inebolu,Kastamonu,Küre,Pinarbasi,Seydiler,Senpazar,Tasköprü,Tosya",
            "38:Akkisla,Bünyan,Develi,Felahiye,Hacilar,Incesu,Kocasinan,Melikgazi,Özvatan(Çukur),Pinarbasi,Sarioglan,Sariz,Talas,Tomarza,Yahyali,Yesilhisar",
            "39:Babaeski,Demirköy,Kirklareli,Kofçaz,Lüleburgaz,Pehlivanköy,Pinarhisar,Vize",
            "40:Akçakent,Akpinar,Boztepe,Çiçekdagi,Kaman,Kirsehir,Mucur",
            "41:Basiskele,Çayirova,Darica,Derince,Dilovasi,Gebze,Gölcük,Izmit,Kandira,Karamürsel,Kartepe,Tütünçiftlik",
            "42:Ahirli,Akören,Aksehir,Altinekin,Beysehir,Bozkir,Cihanbeyli,Çeltik,Çumra,Derbent,Derebucak,Doganhisar,Emirgazi,Eregli,Güneysinir,Hadim,Halkapinar,Hüyük,Ilgin,Kadinhani,Karapinar,Karatay,Kulu,Meram,Sarayönü,Selçuklu,Seydisehir,Taskent,Tuzlukçu,Yalihüyük,Yunak",
            "43:Altintas,Aslanapa,Çavdarhisar,Domaniç,Dumlupinar,Emet,Gediz,Hisarcik,Kütahya,Pazarlar,Simav,Saphane,Tavsanli,Tunçbilek",
            "44:Akçadag,Arapkir,Arguvan,Battalgazi,Darende,Dogansehir,Doganyol,Hekimhan,Kale,Kuluncak,Malatya,Pötürge,Yazihan,Yesilyurt",
            "45:Ahmetli,Akhisar,Alasehir,Demirci,Gölmarmara,Gördes,Kirkagaç,Köprübasi,Kula,Manisa,Salihli,Sarigöl,Saruhanli,Selendi,Soma,Turgutlu",
            "46:Afsin,Andirin,Çaglayancerit,Ekinözü,Elbistan,Göksun,Kahramanmaras,Nurhak,Pazarcik,Türkoglu",
            "47:Dargeçit,Derik,Kiziltepe,Mardin,Mazidagi,Midyat(Estel),Nusaybin,Ömerli,Savur,Yesilli",
            "48:Bodrum,Dalaman,Datça,Fethiye,Kavaklidere,Köycegiz,Marmaris,Milas,Mugla,Ortaca,Ula,Yatagan",
            "49:Bulanik,Hasköy,Korkut,Malazgirt,Mus,Varto",
            "50:Acigöl,Avanos,Derinkuyu,Gülsehir,Hacibektas,Kozakli,Nevsehir,Ürgüp",
            "51:Altunhisar,Bor,Çamardi,Çiftlik(Özyurt),Nigde,Ulukisla",
            "52:Akkus,Aybasti,Çamas,Çatalpinar,Çaybasi,Fatsa,Gölköy,Gülyali,Gürgentepe,Ikizce,Kabatas,Karadüz(Kabadüz),Korgan,Kumru,Mesudiye,Ordu,Persembe,Ulubey,Ünye",
            "53:Ardesen,Çamlihemsin,Çayeli,Derepazari,Findikli,Güneysu,Hemsin,Ikizdere,Iyidere,Kalkandere,Pazar,Rize",
            "54:Adapazari,Akyazi,Arifiye,Erenler,Ferizli,Geyve,Hendek,Karapürçek,Karasu,Kaynarca,Kocaali,Pamukova,Sapanca,Serdivan,Sögütlü,Tarakli",
            "55:19 Mayis(Ballica),Alaçam,Asarcik,Atakum,Ayvacik,Bafra,Canik,Çarsamba,Havza,Ilkadim,Kavak,Ladik,Salipazari,Tekkeköy,Terme,Vezirköprü,Yakakent",
            "56:Aydinlar,Baykan,Eruh,Kurtalan,Pervari,Siirt,Sirvan",
            "57:Ayancik,Boyabat,Dikmen,Duragan,Erfelek,Gerze,Saraydüzü,Sinop,Türkeli",
            "58:Akincilar,Altinyayla,Divrigi,Dogansar,Gemerek,Gölova,Gürün,Hafik,Imranli,Kangal,Koyulhisar,Sivas,Susehri,Sarkisla,Ulas,Yildizeli,Zara",
            "59:Çerkezköy,Çorlu,Hayrabolu,Malkara,Marmaraereglisi,Muratli,Saray,Sarköy,Tekirdag",
            "60:Almus,Artova,Basçiftlik,Erbaa,Niksar,Pazar,Resadiye,Sulusaray,Tokat,Turhal,Yesilyurt,Zile",
            "61:Akçaabat,Arakli,Arsin,Besikdüzü,Çarsibasi,Çaykara,Dernekpazari,Düzköy,Hayrat,Köprübasi,Maçka,Of,Sürmene,Salpazari,Tonya,Trabzon,Vakfikebir,Yomra",
            "62:Çemisgezek,Hozat,Mazgirt,Nazimiye,Ovacik,Pertek,Pülümür,Tunceli",
            "63:Akçakale,Birecik,Bozova,Ceylanpinar,Halfeti,Harran,Hilvan,Siverek,Suruç,Sanliurfa,Viransehir",
            "64:Banaz,Esme,Karahalli,Sivasli,Ulubey,Usak",
            "65:Bahçesaray,Baskale,Çaldiran,Çatak,Edremit(Gümüsdere),Ercis,Gevas,Gürpinar,Muradiye,Özalp,Saray,Van",
            "66:Akdagmadeni,Aydincik,Bogazliyan,Çandir,Çayiralan,Çekerek,Kadisehri,Saraykent,Sarikaya,Sorgun,Sefaatli,Yenifakili,Yerköy,Yozgat",
            "67:Alapli,Çaycuma,Devrek,Gökçebey,Karadenizeregli,Zonguldak",
            "68:Agaçören,Aksaray,Eskil,Gülagaç(Agaçli),Güzelyurt,Ortaköy,Sariyahsi",
            "69:Aydintepe,Bayburt,Demirözü",
            "70:Ayranci,Basyayla,Ermenek,Karaman,Kazimkarabekir,Sariveliler",
            "71:Bahsili,Baliseyh,Çelebi,Delice,Karakeçili,Keskin,Kirikkale,Sulakyurt,Yahsihan",
            "72:Batman,Besiri,Gercüs,Hasankeyf,Kozluk,Sason",
            "73:Beytüssebap,Cizre,Güçlükonak,Idil,Silopi,Sirnak,Uludere",
            "74:Amasra,Bartin,Kurucasile,Ulus",
            "75:Ardahan,Çildir,Damal,Göle,Hanak,Posof",
            "76:Aralik,Igdir,Karakoyunlu,Tuzluca",
            "77:Altinova,Armutlu,Çinarcik,Çiftlikköy,Termal,Yalova",
            "78:Eflani,Eskipazar,Karabük,Ovacik,Safranbolu,Yenice",
            "79:Elbeyli,Kilis,Musabeyli,Polateli",
            "80:Bahçe,Düziçi,Hasanbeyli,Kadirli,Osmaniye,Sumbas,Toprakkale",
            "81:Akçakoca,Cumayeri,Çilimli,Düzce,Gölyaka,Gümüsova,Kaynasli,Yigilca"
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
