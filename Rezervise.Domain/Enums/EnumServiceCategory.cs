namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Hizmet kategorileri - 10xx bazında gruplandırılmış
    /// 10xx: Berber & Kuaför
    /// 11xx: Dış Hekimliği
    /// 12xx: Dövme & Vücut Sanatı
    /// 13xx: Cilt & Güzellik
    /// 14xx: Masaj & Spa
    /// 15xx: Tırnak & El Bakımı
    /// 16xx: Makyaj & Kas
    /// 17xx: Özel Hizmetler (Epilasyon, Kişisel Bakım)
    /// 18xx: Sağlık & Terapi
    /// 19xx: Diğer
    /// </summary>
    public enum EnumServiceCategory
    {
        // ============= 10xx: BERBER VE KUAFÖR HIZMETLERI =============
        Haircut = 1000,                 // Saç Kesimi
        Shave = 1001,                   // Tıraş / Sakal Kesimi
        Coloring = 1002,                // Saç Boyama
        Styling = 1003,                 // Saç Şekillendirme / Fön
        BeardTrimming = 1004,           // Sakal Düzeltme / Şekillendirme
        HairTransplant = 1005,          // Saç Ekimi
        HairTreatment = 1006,           // Saç Bakımı / Tedavisi
        Perm = 1007,                    // Perma / Dalga
        HairStraightening = 1008,       // Saç Düzlestirme
        HairExtension = 1009,           // Saç Uzatma / Ekstansiyon
        KidsHaircut = 1010,             // Çocuk Saç Kesimi
        BridalHair = 1011,              // Gelin Saçi
        HairRemoval = 1012,             // Epilasyon / Ağda (Başlık)

        // ============= 11xx: DIS HIZMETLERI =============
        DentalCleaning = 1100,          // Dış Temizliği
        Filling = 1101,                 // Dolgu
        Extraction = 1102,              // Dış Çekimi
        Whitening = 1103,               // Dış Beyazlatma
        RootCanal = 1104,               // Kanal Tedavisi
        Crown = 1105,                   // Kuron / Kaplama
        Bridge = 1106,                  // Köprü
        Implant = 1107,                 // Implant
        Orthodontics = 1108,            // Ortodonti / Dış Teli
        Veneer = 1109,                  // Lamina / Porselen Kaplama
        GumTreatment = 1110,            // Dış Eti Tedavisi
        PediatricDentistry = 1111,      // Çocuk Dış Hekimliği
        JawSurgery = 1112,              // Çene Cerrahisi
        Prosthesis = 1113,              // Protez
        DentalCheckup = 1114,           // Genel Dış Muayenesi

        // ============= 12xx: DÖVME VE VÜCUT SANATI =============
        Tattoo = 1200,                  // Dövme
        Piercing = 1201,                // Piercing
        TattooRemoval = 1202,           // Dövme Silme
        TattooCoverUp = 1203,           // Dövme Kapatma
        Microblading = 1204,            // Mikrobladıng / Kas
        PermanentMakeup = 1205,         // Kalıcı Makyaj
        BodyArt = 1206,                 // Vücut Sanatı
        Scarification = 1207,           // Scarification (Cilt Kazıma)

        // ============= 13xx: CILT VE GÜZELLIK =============
        Facial = 1300,                  // Yüz Bakımı
        ChemicalPeel = 1301,            // Kimyasal Peeling
        Microdermabrasion = 1302,       // Mikrodermabrazyon
        AcneTreatment = 1303,           // Akne Tedavisi
        AntiAging = 1304,               // Anti-Aging / Yaşlanma Karşıtı
        SkinAnalysis = 1305,            // Cilt Analizi
        Mesotherapy = 1306,             // Mezoterapi
        PRP = 1307,                     // PRP (Platelet Rich Plasma)
        LaserTreatment = 1308,          // Lazer Tedavisi
        HairRemovalLaser = 1309,        // Lazer Epilasyon
        IPL = 1310,                     // IPL (Yoğun Atımlı Isik)

        // ============= 14xx: MASAJ VE SPA =============
        Massage = 1400,                 // Masaj
        DeepTissueMassage = 1401,       // Derin Doku Masajı
        SportsMassage = 1402,           // Spor Masajı
        Reflexology = 1403,             // Refleksoloji
        ThaiMassage = 1404,             // Tay Masajı
        SwedishMassage = 1405,          // Isveç Masajı
        HotStoneMassage = 1406,         // Sıcak Tas Masajı
        Aromatherapy = 1407,            // Aromaterapi
        Shiatsu = 1408,                 // Shiatsu Masajı
        LymphaticDrainage = 1409,       // Lenf Drenajı

        // ============= 15xx: TIRNAK VE EL BAKIMI =============
        Manicure = 1500,                // Manikür
        Pedicure = 1501,                // Pedikür
        NailArt = 1502,                 // Tırnak Sanatı / Süsleme
        NailExtension = 1503,           // Tırnak Uzatma (Jel/Akrilik)
        GelPolish = 1504,               // Jel Oje
        AcrylicNails = 1505,            // Akrilik Tırnak
        NailRepair = 1506,              // Tırnak Onarımı
        ParaffinTreatment = 1507,        // Parafın Bakımı

        // ============= 16xx: MAKYAJ VE KAS =============
        Makeup = 1600,                  // Makyaj
        BridalMakeup = 1601,            // Gelin Makyajı
        EveningMakeup = 1602,           // Gece Makyajı
        PhotoshootMakeup = 1603,        // Fotoğraf Çekimi Makyajı
        AirbrushMakeup = 1604,          // Airbrush Makyaj
        EyebrowDesign = 1605,           // Kas Tasarımı
        EyelashExtension = 1606,        // Kirpik Uzatma
        EyelashLift = 1607,             // Kirpik Lifting

        // ============= 17xx: ÖZEL HIZMETLER =============
        Waxing = 1700,                  // Ağda / Epilasyon
        Sugaring = 1701,                // Şeker Ağdaşı
        Threading = 1702,               // İplik Epilasyon
        Electrolysis = 1703,            // Elektroliz (Kalıcı Epilasyon)
        NutritionConsulting = 1704,     // Beslenme Danışmanlığı
        PersonalTraining = 1705,        // Kişisel Antrenör
        Yoga = 1706,                    // Yoga
        Pilates = 1707,                 // Pilates

        // ============= 18xx: SAGLIK VE TERAPI =============
        HealthCheckup = 1800,           // Sağlık Kontrolü
        Vaccination = 1801,             // Ası
        BloodTest = 1802,               // Kan Testi
        PhysicalTherapy = 1803,         // Fizik Tedavi
        Acupuncture = 1804,             // Akupunktur
        Chiropractic = 1805,            // Kayropraktık
        Osteopathy = 1806,              // Osteopatı
        Psychotherapy = 1807,           // Psikoterapi
        Counseling = 1808,              // Danışmanlık

        // ============= 19xx: DIGER =============
        Consultation = 1900,            // Danışma / Konsültasyon
        Other = 1999                    // Diğer Hizmetler
    }
}