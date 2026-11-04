namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Hizmet kategorileri - 10xx bazında gruplandırılmış
    /// 10xx: Berber & Kuaför
    /// 11xx: Diş Hekimliği
    /// 12xx: Dövme & Vücut Sanatı
    /// 13xx: Cilt & Güzellik
    /// 14xx: Masaj & Spa
    /// 15xx: Tırnak & El Bakımı
    /// 16xx: Makyaj & Kaş
    /// 17xx: Özel Hizmetler (Epilasyon, Kişisel Bakım)
    /// 18xx: Sağlık & Terapi
    /// 19xx: Diğer
    /// </summary>
    public enum EnumServiceCategory
    {
        // ============= 10xx: BERBER VE KUAFÖR HİZMETLERİ =============
        Haircut = 1000,                 // Saç Kesimi
        Shave = 1001,                   // Tıraş / Sakal Kesimi
        Coloring = 1002,                // Saç Boyama
        Styling = 1003,                 // Saç Şekillendirme / Fön
        BeardTrimming = 1004,           // Sakal Düzeltme / Şekillendirme
        HairTransplant = 1005,          // Saç Ekimi
        HairTreatment = 1006,           // Saç Bakımı / Tedavisi
        Perm = 1007,                    // Perma / Dalga
        HairStraightening = 1008,       // Saç Düzleştirme
        HairExtension = 1009,           // Saç Uzatma / Ekstansiyon
        KidsHaircut = 1010,             // Çocuk Saç Kesimi
        BridalHair = 1011,              // Gelin Saçı
        HairRemoval = 1012,             // Epilasyon / Ağda (Başlık)

        // ============= 11xx: DİŞ HİZMETLERİ =============
        DentalCleaning = 1100,          // Diş Temizliği
        Filling = 1101,                 // Dolgu
        Extraction = 1102,              // Diş Çekimi
        Whitening = 1103,               // Diş Beyazlatma
        RootCanal = 1104,               // Kanal Tedavisi
        Crown = 1105,                   // Kuron / Kaplama
        Bridge = 1106,                  // Köprü
        Implant = 1107,                 // İmplant
        Orthodontics = 1108,            // Ortodonti / Diş Teli
        Veneer = 1109,                  // Lamina / Porselen Kaplama
        GumTreatment = 1110,            // Diş Eti Tedavisi
        PediatricDentistry = 1111,      // Çocuk Diş Hekimliği
        JawSurgery = 1112,              // Çene Cerrahisi
        Prosthesis = 1113,              // Protez
        DentalCheckup = 1114,           // Genel Diş Muayenesi

        // ============= 12xx: DÖVME VE VÜCUT SANATI =============
        Tattoo = 1200,                  // Dövme
        Piercing = 1201,                // Piercing
        TattooRemoval = 1202,           // Dövme Silme
        TattooCoverUp = 1203,           // Dövme Kapatma
        Microblading = 1204,            // Mikroblading / Kaş
        PermanentMakeup = 1205,         // Kalıcı Makyaj
        BodyArt = 1206,                 // Vücut Sanatı
        Scarification = 1207,           // Scarification (Cilt Kazıma)

        // ============= 13xx: CİLT VE GÜZELLİK =============
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
        IPL = 1310,                     // IPL (Yoğun Atımlı Işık)

        // ============= 14xx: MASAJ VE SPA =============
        Massage = 1400,                 // Masaj
        DeepTissueMassage = 1401,       // Derin Doku Masajı
        SportsMassage = 1402,           // Spor Masajı
        Reflexology = 1403,             // Refleksoloji
        ThaiMassage = 1404,             // Tay Masajı
        SwedishMassage = 1405,          // İsveç Masajı
        HotStoneMassage = 1406,         // Sıcak Taş Masajı
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
        ParaffinTreatment = 1507,        // Parafin Bakımı

        // ============= 16xx: MAKYAJ VE KAŞ =============
        Makeup = 1600,                  // Makyaj
        BridalMakeup = 1601,            // Gelin Makyajı
        EveningMakeup = 1602,           // Gece Makyajı
        PhotoshootMakeup = 1603,        // Fotoğraf Çekimi Makyajı
        AirbrushMakeup = 1604,          // Airbrush Makyaj
        EyebrowDesign = 1605,           // Kaş Tasarımı
        EyelashExtension = 1606,        // Kirpik Uzatma
        EyelashLift = 1607,             // Kirpik Lifting

        // ============= 17xx: ÖZEL HİZMETLER =============
        Waxing = 1700,                  // Ağda / Epilasyon
        Sugaring = 1701,                // Şeker Ağdası
        Threading = 1702,               // İplik Epilasyon
        Electrolysis = 1703,            // Elektroliz (Kalıcı Epilasyon)
        NutritionConsulting = 1704,     // Beslenme Danışmanlığı
        PersonalTraining = 1705,        // Kişisel Antrenör
        Yoga = 1706,                    // Yoga
        Pilates = 1707,                 // Pilates

        // ============= 18xx: SAĞLIK VE TERAPİ =============
        HealthCheckup = 1800,           // Sağlık Kontrolü
        Vaccination = 1801,             // Aşı
        BloodTest = 1802,               // Kan Testi
        PhysicalTherapy = 1803,         // Fizik Tedavi
        Acupuncture = 1804,             // Akupunktur
        Chiropractic = 1805,            // Kayropraktik
        Osteopathy = 1806,              // Osteopati
        Psychotherapy = 1807,           // Psikoterapi
        Counseling = 1808,              // Danışmanlık

        // ============= 19xx: DİĞER =============
        Consultation = 1900,            // Danışma / Konsültasyon
        Other = 1999                    // Diğer Hizmetler
    }
}