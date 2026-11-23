using Rezervise.Domain.Entities;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    public static class ServiceCategorySeed
    {
        // ===== ROOT KATEGORI GUID'LERI =====
        public static readonly Guid HairServicesId = Guid.Parse("f1000001-0000-0000-0000-000000000001");
        public static readonly Guid BeardShaveId = Guid.Parse("f1000002-0000-0000-0000-000000000002");
        public static readonly Guid DentalServicesId = Guid.Parse("f1000003-0000-0000-0000-000000000003");
        public static readonly Guid TattooPiercingId = Guid.Parse("f1000004-0000-0000-0000-000000000004");
        public static readonly Guid SkinBeautyId = Guid.Parse("f1000005-0000-0000-0000-000000000005");
        public static readonly Guid MassageSpaId = Guid.Parse("f1000006-0000-0000-0000-000000000006");
        public static readonly Guid NailCareId = Guid.Parse("f1000007-0000-0000-0000-000000000007");
        public static readonly Guid MakeupId = Guid.Parse("f1000008-0000-0000-0000-000000000008");
        public static readonly Guid HairRemovalId = Guid.Parse("f1000009-0000-0000-0000-000000000009");
        public static readonly Guid HealthTherapyId = Guid.Parse("f1000010-0000-0000-0000-000000000010");

        // ===== LEAF KATEGORI GUID'LERI =====
        public static readonly Guid HaircutId = Guid.Parse("f2000001-0000-0000-0000-000000000001");
        public static readonly Guid ColoringId = Guid.Parse("f2000002-0000-0000-0000-000000000002");
        public static readonly Guid StylingId = Guid.Parse("f2000003-0000-0000-0000-000000000003");
        public static readonly Guid HairTreatmentId = Guid.Parse("f2000004-0000-0000-0000-000000000004");

        public static readonly Guid BeardTrimId = Guid.Parse("f2000005-0000-0000-0000-000000000005");
        public static readonly Guid ShaveId = Guid.Parse("f2000006-0000-0000-0000-000000000006");

        public static readonly Guid DentalCleaningId = Guid.Parse("f2000007-0000-0000-0000-000000000007");
        public static readonly Guid FillingId = Guid.Parse("f2000008-0000-0000-0000-000000000008");
        public static readonly Guid ExtractionId = Guid.Parse("f2000009-0000-0000-0000-000000000009");
        public static readonly Guid RootCanalId = Guid.Parse("f2000010-0000-0000-0000-000000000010");
        public static readonly Guid WhiteningId = Guid.Parse("f2000011-0000-0000-0000-000000000011");

        public static readonly Guid SmallTattooId = Guid.Parse("f2000012-0000-0000-0000-000000000012");
        public static readonly Guid MediumTattooId = Guid.Parse("f2000013-0000-0000-0000-000000000013");
        public static readonly Guid LargeTattooId = Guid.Parse("f2000014-0000-0000-0000-000000000014");
        public static readonly Guid PiercingId = Guid.Parse("f2000015-0000-0000-0000-000000000015");
        public static readonly Guid TattooRemovalId = Guid.Parse("f2000016-0000-0000-0000-000000000016");

        public static readonly Guid FacialId = Guid.Parse("f2000017-0000-0000-0000-000000000017");
        public static readonly Guid ChemicalPeelId = Guid.Parse("f2000018-0000-0000-0000-000000000018");
        public static readonly Guid AcneTreatmentId = Guid.Parse("f2000019-0000-0000-0000-000000000019");
        public static readonly Guid SkinAnalysisId = Guid.Parse("f2000020-0000-0000-0000-000000000020");

        public static readonly Guid SwedishMassageId = Guid.Parse("f2000021-0000-0000-0000-000000000021");
        public static readonly Guid DeepTissueId = Guid.Parse("f2000022-0000-0000-0000-000000000022");
        public static readonly Guid ThaiMassageId = Guid.Parse("f2000023-0000-0000-0000-000000000023");
        public static readonly Guid HotStoneId = Guid.Parse("f2000024-0000-0000-0000-000000000024");
        public static readonly Guid AromatherapyId = Guid.Parse("f2000025-0000-0000-0000-000000000025");

        public static readonly Guid ManicureId = Guid.Parse("f2000026-0000-0000-0000-000000000026");
        public static readonly Guid PedicureId = Guid.Parse("f2000027-0000-0000-0000-000000000027");
        public static readonly Guid NailArtId = Guid.Parse("f2000028-0000-0000-0000-000000000028");
        public static readonly Guid GelPolishId = Guid.Parse("f2000029-0000-0000-0000-000000000029");
        public static readonly Guid NailExtensionId = Guid.Parse("f2000030-0000-0000-0000-000000000030");

        public static readonly Guid DailyMakeupId = Guid.Parse("f2000031-0000-0000-0000-000000000031");
        public static readonly Guid BridalMakeupId = Guid.Parse("f2000032-0000-0000-0000-000000000032");
        public static readonly Guid EveningMakeupId = Guid.Parse("f2000033-0000-0000-0000-000000000033");
        public static readonly Guid EyelashExtId = Guid.Parse("f2000034-0000-0000-0000-000000000034");

        public static readonly Guid FullBodyWaxId = Guid.Parse("f2000035-0000-0000-0000-000000000035");
        public static readonly Guid LegWaxId = Guid.Parse("f2000036-0000-0000-0000-000000000036");
        public static readonly Guid UnderarmWaxId = Guid.Parse("f2000037-0000-0000-0000-000000000037");
        public static readonly Guid BikiniWaxId = Guid.Parse("f2000038-0000-0000-0000-000000000038");

        public static readonly Guid PhysiotherapyId = Guid.Parse("f2000039-0000-0000-0000-000000000039");
        public static readonly Guid ChiropracticId = Guid.Parse("f2000040-0000-0000-0000-000000000040");
        public static readonly Guid AcupunctureId = Guid.Parse("f2000041-0000-0000-0000-000000000041");
        public static readonly Guid PsychotherapyId = Guid.Parse("f2000042-0000-0000-0000-000000000042");

        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            if (await context.ServiceCategories.AnyAsync())
            {
                logger.LogInformation("ServiceCategories zaten mevcut, atlanıyor.");
                return;
            }

            logger.LogInformation("ServiceCategories seed ediliyor...");

            // ===== ROOT KATEGORILER =====
            var hairServices = CreateCategory(HairServicesId, 1, "#FF6B6B", null,
                "Saç Hizmetleri", "Hair Services",
                "Saç kesimi, boyama ve bakım hizmetleri", "Haircut, coloring and çare services");

            var beardShave = CreateCategory(BeardShaveId, 2, "#4A5568", null,
                "Sakal & Tıraş", "Beard & Shave",
                "Sakal kesimi ve tıraş hizmetleri", "Beard trimming and shaving services");

            var dentalServices = CreateCategory(DentalServicesId, 3, "#4299E1", null,
                "Dış Hizmetleri", "Dental Services",
                "Dış tedavisi ve bakımı", "Dental treatment and çare");

            var tattooPiercing = CreateCategory(TattooPiercingId, 4, "#9F7AEA", null,
                "Dövme & Piercing", "Tattoo & Piercing",
                "Dövme ve piercing hizmetleri", "Tattoo and piercing services");

            var skinBeauty = CreateCategory(SkinBeautyId, 5, "#ED8936", null,
                "Cilt & Güzellik", "Skin & Beauty",
                "Cilt bakımı ve güzellik hizmetleri", "Skin çare and beauty services");

            var massageSpa = CreateCategory(MassageSpaId, 6, "#38B2AC", null,
                "Masaj & Spa", "Massage & Spa",
                "Masaj ve spa hizmetleri", "Massage and spa services");

            var nailCare = CreateCategory(NailCareId, 7, "#F687B3", null,
                "Tırnak Bakımı", "Nail Çare",
                "Manikür, pedikür ve tırnak sanatı", "Manicure, pedicure and nail art");

            var makeup = CreateCategory(MakeupId, 8, "#FC8181", null,
                "Makyaj", "Makeup",
                "Profesyonel makyaj hizmetleri", "Professional makeup services");

            var hairRemoval = CreateCategory(HairRemovalId, 9, "#68D391", null,
                "Epilasyon & Ağda", "Hair Removal",
                "Epilasyon ve ağda hizmetleri", "Waxing and hair removal services");

            var healthTherapy = CreateCategory(HealthTherapyId, 10, "#F56565", null,
                "Sağlık & Terapi", "Health & Therapy",
                "Sağlık ve terapi hizmetleri", "Health and therapy services");

            // ===== LEAF KATEGORILER =====

            var haircut = CreateLeaf(HaircutId, 1, hairServices,
                "Saç Kesimi", "Haircut",
                "Erkek ve kadın saç kesimi", "Men's and women's haircut");

            var coloring = CreateLeaf(ColoringId, 2, hairServices,
                "Saç Boyama", "Hair Coloring",
                "Saç boyama ve renklendirme", "Hair coloring and dyeing");

            var styling = CreateLeaf(StylingId, 3, hairServices,
                "Saç Şekillendirme", "Hair Styling",
                "Fön, masa ve şekillendirme", "Blow dry, curling and styling");

            var hairTreatment = CreateLeaf(HairTreatmentId, 4, hairServices,
                "Saç Bakımı", "Hair Treatment",
                "Saç bakım ve tedavi hizmetleri", "Hair çare and treatment");

            var beardTrim = CreateLeaf(BeardTrimId, 1, beardShave,
                "Sakal Kesimi", "Beard Trim",
                "Sakal düzeltme ve şekillendirme", "Beard trimming and shaping");

            var shave = CreateLeaf(ShaveId, 2, beardShave,
                "Tıraş", "Shave",
                "Ustura tıraşı ve klasik tıraş", "Razor shave and classic shave");

            var dentalCleaning = CreateLeaf(DentalCleaningId, 1, dentalServices,
                "Dış Temizliği", "Dental Cleaning",
                "Dış taşı temizliği ve polisaj", "Scaling and polishing");

            var filling = CreateLeaf(FillingId, 2, dentalServices,
                "Dolgu", "Filling",
                "Kompozit ve amalgam dolgu", "Composite and amalgam filling");

            var extraction = CreateLeaf(ExtractionId, 3, dentalServices,
                "Dış Çekimi", "Tooth Extraction",
                "Basit ve cerrahi dış çekimi", "Simple and surgical extraction");

            var rootCanal = CreateLeaf(RootCanalId, 4, dentalServices,
                "Kanal Tedavisi", "Root Canal",
                "Endodontik tedavi", "Endodontic treatment");

            var whitening = CreateLeaf(WhiteningId, 5, dentalServices,
                "Dış Beyazlatma", "Teeth Whitening",
                "Ofis tipi beyazlatma", "İn-office whitening");

            var smallTattoo = CreateLeaf(SmallTattooId, 1, tattooPiercing,
                "Küçük Dövme", "Small Tattoo",
                "5cm'e kadar dövme", "Tattoo up to 5cm");

            var mediumTattoo = CreateLeaf(MediumTattooId, 2, tattooPiercing,
                "Orta Dövme", "Medium Tattoo",
                "5-15cm arası dövme", "Tattoo 5-15cm");

            var largeTattoo = CreateLeaf(LargeTattooId, 3, tattooPiercing,
                "Büyük Dövme", "Large Tattoo",
                "15cm üzeri dövme", "Tattoo over 15cm");

            var piercing = CreateLeaf(PiercingId, 4, tattooPiercing,
                "Piercing", "Piercing",
                "Vücut piercing hizmetleri", "Body piercing services");

            var tattooRemoval = CreateLeaf(TattooRemovalId, 5, tattooPiercing,
                "Dövme Silme", "Tattoo Removal",
                "Lazer ile dövme silme", "Laser tattoo removal");

            var facial = CreateLeaf(FacialId, 1, skinBeauty,
                "Yüz Bakımı", "Facial",
                "Profesyonel yüz bakımı", "Professional facial çare");

            var chemicalPeel = CreateLeaf(ChemicalPeelId, 2, skinBeauty,
                "Kimyasal Peeling", "Chemical Peel",
                "Cilt yenileme peeling", "Skin renewal peel");

            var acneTreatment = CreateLeaf(AcneTreatmentId, 3, skinBeauty,
                "Akne Tedavisi", "Acne Treatment",
                "Akne ve sivilce tedavisi", "Acne treatment");

            var skinAnalysis = CreateLeaf(SkinAnalysisId, 4, skinBeauty,
                "Cilt Analizi", "Skin Analysis",
                "Detaylı cilt analizi", "Detailed skin analysis");

            var swedishMassage = CreateLeaf(SwedishMassageId, 1, massageSpa,
                "Isveç Masajı", "Swedish Massage",
                "Klasik Isveç masajı", "Classic Swedish massage");

            var deepTissue = CreateLeaf(DeepTissueId, 2, massageSpa,
                "Derin Doku Masajı", "Deep Tissue Massage",
                "Derin doku masajı", "Deep tissue massage");

            var thaiMassage = CreateLeaf(ThaiMassageId, 3, massageSpa,
                "Tay Masajı", "Thai Massage",
                "Geleneksel Tay masajı", "Traditional Thai massage");

            var hotStone = CreateLeaf(HotStoneId, 4, massageSpa,
                "Sıcak Tas Masajı", "Hot Stone Massage",
                "Sıcak tas masajı", "Hot stone massage");

            var aromatherapy = CreateLeaf(AromatherapyId, 5, massageSpa,
                "Aromaterapi", "Aromatherapy",
                "Aromaterapi masajı", "Aromatherapy massage");

            var manicure = CreateLeaf(ManicureId, 1, nailCare,
                "Manikür", "Manicure",
                "Klasik ve spa manikür", "Classic and spa manicure");

            var pedicure = CreateLeaf(PedicureId, 2, nailCare,
                "Pedikür", "Pedicure",
                "Klasik ve spa pedikür", "Classic and spa pedicure");

            var nailArt = CreateLeaf(NailArtId, 3, nailCare,
                "Tırnak Sanatı", "Nail Art",
                "Nail art ve süsleme", "Nail art and decoration");

            var gelPolish = CreateLeaf(GelPolishId, 4, nailCare,
                "Kalıcı Oje", "Gel Polish",
                "Gel ve kalıcı oje", "Gel and permanent polish");

            var nailExtension = CreateLeaf(NailExtensionId, 5, nailCare,
                "Tırnak Uzatma", "Nail Extension",
                "Protez tırnak ve uzatma", "Prosthetic nails and extensions");

            var dailyMakeup = CreateLeaf(DailyMakeupId, 1, makeup,
                "Günlük Makyaj", "Daily Makeup",
                "Günlük makyaj uygulaması", "Daily makeup application");

            var bridalMakeup = CreateLeaf(BridalMakeupId, 2, makeup,
                "Gelin Makyajı", "Bridal Makeup",
                "Gelin makyajı", "Bridal makeup");

            var eveningMakeup = CreateLeaf(EveningMakeupId, 3, makeup,
                "Gece Makyajı", "Evening Makeup",
                "Özel davet ve gece makyajı", "Special event and evening makeup");

            var eyelashExt = CreateLeaf(EyelashExtId, 4, makeup,
                "Kirpik Uzatma", "Eyelash Extension",
                "İpek kirpik ve uzatma", "Silk lash and extensions");

            var fullBodyWax = CreateLeaf(FullBodyWaxId, 1, hairRemoval,
                "Tam Vücut Ağda", "Full Body Wax",
                "Tam vücut ağda", "Full body waxing");

            var legWax = CreateLeaf(LegWaxId, 2, hairRemoval,
                "Bacak Ağda", "Leg Wax",
                "Bacak ağda", "Leg waxing");

            var underarmWax = CreateLeaf(UnderarmWaxId, 3, hairRemoval,
                "Koltuk Altı Ağda", "Underarm Wax",
                "Koltuk altı ağda", "Underarm waxing");

            var bikiniWax = CreateLeaf(BikiniWaxId, 4, hairRemoval,
                "Bikini Ağda", "Bikini Wax",
                "Bikini bölgesi ağda", "Bikini area waxing");

            var physiotherapy = CreateLeaf(PhysiotherapyId, 1, healthTherapy,
                "Fizyoterapi", "Physiotherapy",
                "Fizik tedavi ve rehabilitasyon", "Physical therapy and rehabilitation");

            var chiropractic = CreateLeaf(ChiropracticId, 2, healthTherapy,
                "Kayropraktık", "Chiropractic",
                "Kayropraktık tedavi", "Chiropractic treatment");

            var acupuncture = CreateLeaf(AcupunctureId, 3, healthTherapy,
                "Akupunktur", "Acupuncture",
                "Geleneksel akupunktur", "Traditional acupuncture");

            var psychotherapy = CreateLeaf(PsychotherapyId, 4, healthTherapy,
                "Psikoterapi", "Psychotherapy",
                "Psikolojik danışmanlık", "Psychological counseling");

            var allCategories = new List<ServiceCategory>
            {
                hairServices, beardShave, dentalServices, tattooPiercing, skinBeauty,
                massageSpa, nailCare, makeup, hairRemoval, healthTherapy,
                haircut, coloring, styling, hairTreatment,
                beardTrim, shave,
                dentalCleaning, filling, extraction, rootCanal, whitening,
                smallTattoo, mediumTattoo, largeTattoo, piercing, tattooRemoval,
                facial, chemicalPeel, acneTreatment, skinAnalysis,
                swedishMassage, deepTissue, thaiMassage, hotStone, aromatherapy,
                manicure, pedicure, nailArt, gelPolish, nailExtension,
                dailyMakeup, bridalMakeup, eveningMakeup, eyelashExt,
                fullBodyWax, legWax, underarmWax, bikiniWax,
                physiotherapy, chiropractic, acupuncture, psychotherapy
            };

            await context.ServiceCategories.AddRangeAsync(allCategories);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "ServiceCategories seed edildi: {Count} kategori ({Root} root + {Leaf} leaf)",
                allCategories.Count, 10, allCategories.Count - 10);
        }

        private static ServiceCategory CreateCategory(Guid id, int displayOrder, string colorHex,
            string? iconUrl, string nameTr, string nameEn, string? descTr, string? descEn)
        {
            var category = new ServiceCategory(id, displayOrder);
            category.SetColorHex(colorHex);
            if (iconUrl != null)
                category.SetIconUrl(iconUrl);
            category.AddTranslation("tr", nameTr, descTr);
            category.AddTranslation("en", nameEn, descEn);
            return category;
        }

        private static ServiceCategory CreateLeaf(Guid id, int displayOrder,
            ServiceCategory parent, string nameTr, string nameEn, string? descTr, string? descEn)
        {
            var category = new ServiceCategory(id, displayOrder);
            category.SetColorHex(parent.ColorHex);
            category.SetParentCategory(parent);
            category.AddTranslation("tr", nameTr, descTr);
            category.AddTranslation("en", nameEn, descEn);
            return category;
        }
    }
}