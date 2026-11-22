namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Hizmet kategorileri - 10xx bazinda gruplandirilmis
    /// 10xx: Berber & Kuaför
    /// 11xx: Dis Hekimligi
    /// 12xx: Dövme & Vücut Sanati
    /// 13xx: Cilt & Güzellik
    /// 14xx: Masaj & Spa
    /// 15xx: Tirnak & El Bakimi
    /// 16xx: Makyaj & Kas
    /// 17xx: Özel Hizmetler (Epilasyon, Kisisel Bakim)
    /// 18xx: Saglik & Terapi
    /// 19xx: Diger
    /// </summary>
    public enum EnumServiceCategory
    {
        // ============= 10xx: BERBER VE KUAFÖR HIZMETLERI =============
        Haircut = 1000,                 // Saç Kesimi
        Shave = 1001,                   // Tiras / Sakal Kesimi
        Coloring = 1002,                // Saç Boyama
        Styling = 1003,                 // Saç Sekillendirme / Fön
        BeardTrimming = 1004,           // Sakal Düzeltme / Sekillendirme
        HairTransplant = 1005,          // Saç Ekimi
        HairTreatment = 1006,           // Saç Bakimi / Tedavisi
        Perm = 1007,                    // Perma / Dalga
        HairStraightening = 1008,       // Saç Düzlestirme
        HairExtension = 1009,           // Saç Uzatma / Ekstansiyon
        KidsHaircut = 1010,             // Çocuk Saç Kesimi
        BridalHair = 1011,              // Gelin Saçi
        HairRemoval = 1012,             // Epilasyon / Agda (Baslik)

        // ============= 11xx: DIS HIZMETLERI =============
        DentalCleaning = 1100,          // Dis Temizligi
        Filling = 1101,                 // Dolgu
        Extraction = 1102,              // Dis Çekimi
        Whitening = 1103,               // Dis Beyazlatma
        RootCanal = 1104,               // Kanal Tedavisi
        Crown = 1105,                   // Kuron / Kaplama
        Bridge = 1106,                  // Köprü
        Implant = 1107,                 // Implant
        Orthodontics = 1108,            // Ortodonti / Dis Teli
        Veneer = 1109,                  // Lamina / Porselen Kaplama
        GumTreatment = 1110,            // Dis Eti Tedavisi
        PediatricDentistry = 1111,      // Çocuk Dis Hekimligi
        JawSurgery = 1112,              // Çene Cerrahisi
        Prosthesis = 1113,              // Protez
        DentalCheckup = 1114,           // Genel Dis Muayenesi

        // ============= 12xx: DÖVME VE VÜCUT SANATI =============
        Tattoo = 1200,                  // Dövme
        Piercing = 1201,                // Piercing
        TattooRemoval = 1202,           // Dövme Silme
        TattooCoverUp = 1203,           // Dövme Kapatma
        Microblading = 1204,            // Mikroblading / Kas
        PermanentMakeup = 1205,         // Kalici Makyaj
        BodyArt = 1206,                 // Vücut Sanati
        Scarification = 1207,           // Scarification (Cilt Kazima)

        // ============= 13xx: CILT VE GÜZELLIK =============
        Facial = 1300,                  // Yüz Bakimi
        ChemicalPeel = 1301,            // Kimyasal Peeling
        Microdermabrasion = 1302,       // Mikrodermabrazyon
        AcneTreatment = 1303,           // Akne Tedavisi
        AntiAging = 1304,               // Anti-Aging / Yaslanma Karsiti
        SkinAnalysis = 1305,            // Cilt Analizi
        Mesotherapy = 1306,             // Mezoterapi
        PRP = 1307,                     // PRP (Platelet Rich Plasma)
        LaserTreatment = 1308,          // Lazer Tedavisi
        HairRemovalLaser = 1309,        // Lazer Epilasyon
        IPL = 1310,                     // IPL (Yogun Atimli Isik)

        // ============= 14xx: MASAJ VE SPA =============
        Massage = 1400,                 // Masaj
        DeepTissueMassage = 1401,       // Derin Doku Masaji
        SportsMassage = 1402,           // Spor Masaji
        Reflexology = 1403,             // Refleksoloji
        ThaiMassage = 1404,             // Tay Masaji
        SwedishMassage = 1405,          // Isveç Masaji
        HotStoneMassage = 1406,         // Sicak Tas Masaji
        Aromatherapy = 1407,            // Aromaterapi
        Shiatsu = 1408,                 // Shiatsu Masaji
        LymphaticDrainage = 1409,       // Lenf Drenaji

        // ============= 15xx: TIRNAK VE EL BAKIMI =============
        Manicure = 1500,                // Manikür
        Pedicure = 1501,                // Pedikür
        NailArt = 1502,                 // Tirnak Sanati / Süsleme
        NailExtension = 1503,           // Tirnak Uzatma (Jel/Akrilik)
        GelPolish = 1504,               // Jel Oje
        AcrylicNails = 1505,            // Akrilik Tirnak
        NailRepair = 1506,              // Tirnak Onarimi
        ParaffinTreatment = 1507,        // Parafin Bakimi

        // ============= 16xx: MAKYAJ VE KAS =============
        Makeup = 1600,                  // Makyaj
        BridalMakeup = 1601,            // Gelin Makyaji
        EveningMakeup = 1602,           // Gece Makyaji
        PhotoshootMakeup = 1603,        // Fotograf Çekimi Makyaji
        AirbrushMakeup = 1604,          // Airbrush Makyaj
        EyebrowDesign = 1605,           // Kas Tasarimi
        EyelashExtension = 1606,        // Kirpik Uzatma
        EyelashLift = 1607,             // Kirpik Lifting

        // ============= 17xx: ÖZEL HIZMETLER =============
        Waxing = 1700,                  // Agda / Epilasyon
        Sugaring = 1701,                // Seker Agdasi
        Threading = 1702,               // Iplik Epilasyon
        Electrolysis = 1703,            // Elektroliz (Kalici Epilasyon)
        NutritionConsulting = 1704,     // Beslenme Danismanligi
        PersonalTraining = 1705,        // Kisisel Antrenör
        Yoga = 1706,                    // Yoga
        Pilates = 1707,                 // Pilates

        // ============= 18xx: SAGLIK VE TERAPI =============
        HealthCheckup = 1800,           // Saglik Kontrolü
        Vaccination = 1801,             // Asi
        BloodTest = 1802,               // Kan Testi
        PhysicalTherapy = 1803,         // Fizik Tedavi
        Acupuncture = 1804,             // Akupunktur
        Chiropractic = 1805,            // Kayropraktik
        Osteopathy = 1806,              // Osteopati
        Psychotherapy = 1807,           // Psikoterapi
        Counseling = 1808,              // Danismanlik

        // ============= 19xx: DIGER =============
        Consultation = 1900,            // Danisma / Konsültasyon
        Other = 1999                    // Diger Hizmetler
    }
}