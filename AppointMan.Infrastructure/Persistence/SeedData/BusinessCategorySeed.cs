using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// Isletme kategorilerini ana/alt kategori hiyerarsisi olarak ekler veya gunceller.
    /// Idempotenttir: var olan kayit Id ile bulunup guncellenir, yoksa eklenir.
    /// Boylece mevcut isletme kayitlarinin kategori baglantilari bozulmaz.
    /// </summary>
    public static class BusinessCategorySeed
    {
        // Mevcut sabit GUID'ler korunur; bu kayitlar alt kategori olarak yeni
        // ana kategorilere baglanir. Business tablosundaki FK'lar degismez.
        public static readonly Guid BarberId = Guid.Parse("a1000001-0000-0000-0000-000000000001");
        public static readonly Guid DentistId = Guid.Parse("a1000002-0000-0000-0000-000000000002");
        public static readonly Guid TattooId = Guid.Parse("a1000003-0000-0000-0000-000000000003");
        public static readonly Guid WaxingId = Guid.Parse("a1000004-0000-0000-0000-000000000004");
        public static readonly Guid HairdresserId = Guid.Parse("a1000005-0000-0000-0000-000000000005");
        public static readonly Guid NailSalonId = Guid.Parse("a1000006-0000-0000-0000-000000000006");
        public static readonly Guid SpaId = Guid.Parse("a1000007-0000-0000-0000-000000000007");
        public static readonly Guid MassageId = Guid.Parse("a1000008-0000-0000-0000-000000000008");

        // Ana kategoriler (12 adet).
        public static readonly Guid BeautyRootId = RootId(1);
        public static readonly Guid HealthRootId = RootId(2);
        public static readonly Guid SportsRootId = RootId(3);
        public static readonly Guid EducationRootId = RootId(4);
        public static readonly Guid AutomotiveRootId = RootId(5);
        public static readonly Guid HomeRootId = RootId(6);
        public static readonly Guid LegalRootId = RootId(7);
        public static readonly Guid PhotoRootId = RootId(8);
        public static readonly Guid PetRootId = RootId(9);
        public static readonly Guid TravelRootId = RootId(10);
        public static readonly Guid FashionRootId = RootId(11);
        public static readonly Guid OtherRootId = RootId(12);

        /// <summary>
        /// Kategori hiyerarsisini seed eder. Mevcut kayitlar silinmez; yalnizca
        /// ad/aciklama/siralama/ust kategori bilgileri guncellenir.
        /// </summary>
        /// <param name="context">Veritabani baglami.</param>
        /// <param name="logger">Log yazici.</param>
        /// <returns>Islem tamamlandiginda tamamlanan gorev.</returns>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            var nodes = BuildNodes();

            // IgnoreQueryFilters: daha once pasiflestirilmis kayitlar da guncellenir,
            // aksi halde ayni Id ile ikinci kayit eklenmeye calisilirdi.
            var existing = await context.BusinessCategories
                .IgnoreQueryFilters()
                .ToListAsync();

            var byId = existing.ToDictionary(c => c.Id);

            // Once ana kategoriler, sonra alt kategoriler: parent nesnesi elde
            // olmadan SetParentCategory cagrilamaz.
            foreach (var node in nodes.OrderBy(n => n.ParentId.HasValue ? 1 : 0))
            {
                if (!byId.TryGetValue(node.Id, out var category))
                {
                    category = new BusinessCategory(node.Id, node.Name, node.DisplayOrder, node.BusinessType);
                    category.SetDescription(node.Description);
                    context.BusinessCategories.Add(category);
                    byId[node.Id] = category;
                }
                else
                {
                    category.SetName(node.Name);
                    category.SetDescription(node.Description);
                    category.SetDisplayOrder(node.DisplayOrder);
                    category.Activate();
                }

                if (node.ParentId.HasValue &&
                    byId.TryGetValue(node.ParentId.Value, out var parent) &&
                    category.ParentCategoryId != parent.Id)
                {
                    category.SetParentCategory(parent);
                }
            }

            await context.SaveChangesAsync();

            var rootCount = nodes.Count(n => !n.ParentId.HasValue);
            logger.LogInformation(
                "BusinessCategories hiyerarsisi seed edildi: {Root} ana, {Sub} alt kategori",
                rootCount, nodes.Count - rootCount);
        }

        private static Guid RootId(int index) => Guid.Parse($"b1{index:D6}-0000-0000-0000-000000000000");

        private static Guid SubId(int index) => Guid.Parse($"c1{index:D6}-0000-0000-0000-000000000000");

        /// <summary>Tek bir kategori dugumu.</summary>
        private sealed record CategoryNode(
            Guid Id,
            string Name,
            string? Description,
            int DisplayOrder,
            EnumBusinessType? BusinessType,
            Guid? ParentId);

        /// <summary>
        /// Ana + alt kategori listesini uretir. Mevcut 8 kategori Id'si korunur;
        /// boylece isletme baglantilari ve correspondingBusinessType degerleri bozulmaz.
        /// </summary>
        private static List<CategoryNode> BuildNodes()
        {
            var nodes = new List<CategoryNode>();

            // ===== Ana kategoriler =====
            nodes.Add(new CategoryNode(BeautyRootId, "Guzellik ve Kisisel Bakim", "Kuafor, berber, bakim ve wellness hizmetleri", 1, null, null));
            nodes.Add(new CategoryNode(HealthRootId, "Saglik ve Medikal", "Saglik, teshis ve tedavi hizmetleri", 2, null, null));
            nodes.Add(new CategoryNode(SportsRootId, "Spor ve Fitness", "Spor salonlari, stüdyolar ve antrenman hizmetleri", 3, null, null));
            nodes.Add(new CategoryNode(EducationRootId, "Egitim ve Danismanlik", "Kurs, ders ve kariyer danismanligi hizmetleri", 4, null, null));
            nodes.Add(new CategoryNode(AutomotiveRootId, "Otomotiv ve Arac", "Arac bakim, onarim ve ekspertiz hizmetleri", 5, null, null));
            nodes.Add(new CategoryNode(HomeRootId, "Ev ve Teknik", "Ev, tesisat, tadilat ve temizlik hizmetleri", 6, null, null));
            nodes.Add(new CategoryNode(LegalRootId, "Hukuk ve Finans", "Hukuk, muhasebe ve finansal danismanlik hizmetleri", 7, null, null));
            nodes.Add(new CategoryNode(PhotoRootId, "Fotograf ve Organizasyon", "Fotograf, organizasyon ve etkinlik hizmetleri", 8, null, null));
            nodes.Add(new CategoryNode(PetRootId, "Evcil Hayvan", "Veteriner, bakim ve egitim hizmetleri", 9, null, null));
            nodes.Add(new CategoryNode(TravelRootId, "Konaklama ve Turizm", "Konaklama, tur ve seyahat hizmetleri", 10, null, null));
            nodes.Add(new CategoryNode(FashionRootId, "Moda ve Tasarim", "Giyim, terzi ve tasarim hizmetleri", 11, null, null));
            nodes.Add(new CategoryNode(OtherRootId, "Diger Hizmetler", "Diger profesyonel ve kisisel hizmetler", 12, null, null));

            // ===== Alt kategoriler =====
            // Mevcut 8 kayit: Id'ler korunur, adlar yeni listeye uyarlanir,
            // correspondingBusinessType degerleri DEGISTIRILMEZ.
            nodes.Add(new CategoryNode(HairdresserId, "Kuafor", "Kadin kuaforu ve sac hizmetleri", 1, EnumBusinessType.Hairdresser, BeautyRootId));
            nodes.Add(new CategoryNode(BarberId, "Berber", "Erkek kuaforu ve berber hizmetleri", 2, EnumBusinessType.Barber, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 1), "Guzellik Salonu", "Guzellik ve bakim hizmetleri", 3, null, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 2), "Cilt Bakim Merkezi", "Cilt bakimi ve bakim uygulamalari", 4, null, BeautyRootId));
            nodes.Add(new CategoryNode(WaxingId, "Lazer Epilasyon Merkezi", "Epilasyon ve agda hizmetleri", 5, EnumBusinessType.Waxing, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 3), "Kalici Makyaj", "Kalici makyaj uygulamalari", 6, null, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 4), "Protez Tirnak", "Protez tirnak uygulamalari", 7, null, BeautyRootId));
            nodes.Add(new CategoryNode(NailSalonId, "Manikur & Pedikur", "Manikur, pedikur ve tirnak sanati", 8, EnumBusinessType.NailSalon, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 5), "Kirpik & Kas Tasarimi", "Kirpik ve kas tasarimi hizmetleri", 9, null, BeautyRootId));
            nodes.Add(new CategoryNode(MassageId, "Masaj Salonu", "Terapotik masaj hizmetleri", 10, EnumBusinessType.Massage, BeautyRootId));
            nodes.Add(new CategoryNode(SpaId, "Spa & Wellness", "Wellness ve guzellik merkezi", 11, EnumBusinessType.Spa, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 6), "Solaryum", "Solaryum hizmetleri", 12, null, BeautyRootId));
            nodes.Add(new CategoryNode(TattooId, "Dovme & Piercing Studyosu", "Dovme ve vucut sanati", 13, EnumBusinessType.Tattoo, BeautyRootId));

            nodes.Add(new CategoryNode(DentistId, "Dis Hekimi", "Dis sagligi ve tedavi hizmetleri", 1, EnumBusinessType.Dentist, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 1), "Dis Klinigi", "Dis sagligi klinigi", 2, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 2), "Ozel Hastane", "Ozel hastane hizmetleri", 3, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 3), "Tip Merkezi", "Tip merkezi hizmetleri", 4, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 4), "Poliklinik", "Poliklinik hizmetleri", 5, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 5), "Dermatoloji", "Cilt hastaliklari uzmanligi", 6, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 6), "Fizik Tedavi Merkezi", "Fizik tedavi ve rehabilitasyon", 7, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 7), "Psikiyatri", "Psikiyatri uzmanligi", 8, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 8), "Psikolog", "Psikolojik danismanlik", 9, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 9), "Diyetisyen", "Beslenme ve diyet danismanligi", 10, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 10), "Beslenme Danismanligi", "Beslenme danismanligi", 11, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 11), "Goz Hastaliklari", "Goz hastaliklari uzmanligi", 12, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 12), "Kulak Burun Bogaz", "KBB uzmanligi", 13, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 13), "Kadin Hastaliklari ve Dogum", "Kadin hastaliklari ve dogum", 14, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 14), "Cocuk Sagligi", "Cocuk sagligi ve hastaliklari", 15, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 15), "Ortopedi", "Ortopedi ve travmatoloji", 16, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 16), "Kardiyoloji", "Kalp ve damar hastaliklari", 17, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 17), "Uroloji", "Uroloji uzmanligi", 18, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 18), "Noroloji", "Noroloji uzmanligi", 19, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 19), "Genel Cerrahi", "Genel cerrahi hizmetleri", 20, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 20), "Laboratuvar", "Laboratuvar hizmetleri", 21, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 21), "Goruntuleme Merkezi", "Goruntuleme ve radyoloji", 22, null, HealthRootId));

            nodes.Add(new CategoryNode(NewSub(SportsRootId, 1), "Fitness Salonu", "Fitness ve spor salonu", 1, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 2), "Kisisel Antrenor", "Kisisel antrenman hizmetleri", 2, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 3), "Pilates Studyosu", "Pilates dersleri", 3, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 4), "Reformer Pilates", "Reformer pilates dersleri", 4, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 5), "Yoga Studyosu", "Yoga dersleri", 5, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 6), "CrossFit", "CrossFit antrenmanlari", 6, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 7), "Boks Salonu", "Boks antrenmanlari", 7, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 8), "Kick Boks", "Kick boks antrenmanlari", 8, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 9), "Yuzme Kursu", "Yuzme dersleri", 9, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 10), "Dans Studyosu", "Dans dersleri", 10, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 11), "Jimnastik", "Jimnastik dersleri", 11, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 12), "Tenis Kortu", "Tenis kortu ve dersleri", 12, null, SportsRootId));

            nodes.Add(new CategoryNode(NewSub(EducationRootId, 1), "Ozel Ders", "Birebir ozel ders", 1, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 2), "Dil Kursu", "Yabanci dil kurslari", 2, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 3), "Muzik Kursu", "Muzik ve enstruman kurslari", 3, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 4), "Dans Kursu", "Dans kurslari", 4, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 5), "Surucu Kursu", "Ehliyet ve surucu kursu", 5, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 6), "Egitim Danismanligi", "Egitim danismanligi", 6, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 7), "Kariyer Danismanligi", "Kariyer planlama danismanligi", 7, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 8), "Yasam Koclugu", "Yasam kosclugu hizmetleri", 8, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 9), "Ogrenci Koclugu", "Ogrenci kosclugu hizmetleri", 9, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 10), "Sinav Hazirlik Merkezi", "Sinav hazirlik kurslari", 10, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 11), "Bilgisayar Kursu", "Bilgisayar ve yazilim kurslari", 11, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 12), "Sanat Atolyesi", "Sanat atolyeleri", 12, null, EducationRootId));

            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 1), "Oto Servis", "Arac servis hizmetleri", 1, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 2), "Oto Ekspertiz", "Arac ekspertiz hizmetleri", 2, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 3), "Oto Yikama", "Arac yikama hizmetleri", 3, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 4), "Oto Kuafor", "Arac detayli bakim", 4, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 5), "Lastik Servisi", "Lastik degisim ve bakim", 5, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 6), "Arac Bakim Merkezi", "Periyodik arac bakimi", 6, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 7), "Kaporta & Boya", "Kaporta ve boya hizmetleri", 7, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 8), "Cam Filmi", "Arac cam filmi uygulamasi", 8, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 9), "Arac Detayli Temizlik", "Detayli arac temizligi", 9, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 10), "Motosiklet Servisi", "Motosiklet servis hizmetleri", 10, null, AutomotiveRootId));

            nodes.Add(new CategoryNode(NewSub(HomeRootId, 1), "Elektrikci", "Elektrik tesisati ve onarim", 1, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 2), "Tesisatci", "Su tesisati ve onarim", 2, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 3), "Kombi Servisi", "Kombi bakim ve servis", 3, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 4), "Klima Servisi", "Klima bakim ve servis", 4, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 5), "Beyaz Esya Servisi", "Beyaz esya servis hizmetleri", 5, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 6), "Mobilya Montaji", "Mobilya montaj hizmetleri", 6, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 7), "Temizlik Hizmeti", "Ev ve ofis temizligi", 7, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 8), "Hali Yikama", "Hali yikama hizmetleri", 8, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 9), "Ilaclama Hizmeti", "Ilaclama hizmetleri", 9, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 10), "Peyzaj & Bahce Bakimi", "Peyzaj ve bahce bakimi", 10, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 11), "Ic Mimarlik", "Ic mimarlik hizmetleri", 11, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 12), "Dekorasyon Danismanligi", "Dekorasyon danismanligi", 12, null, HomeRootId));

            nodes.Add(new CategoryNode(NewSub(LegalRootId, 1), "Avukatlik Burosu", "Avukatlik ve hukuki temsil", 1, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 2), "Hukuk Danismanligi", "Hukuk danismanligi", 2, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 3), "Mali Musavir", "Mali musavirlik hizmetleri", 3, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 4), "Muhasebe Burosu", "Muhasebe ve defter tutma", 4, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 5), "Finansal Danismanlik", "Finansal planlama danismanligi", 5, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 6), "Sigorta Acentesi", "Sigorta acenteligi", 6, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 7), "Gayrimenkul Danismanligi", "Gayrimenkul danismanligi", 7, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 8), "Emlak Ofisi", "Emlak alim satim hizmetleri", 8, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 9), "Ekspertiz Hizmetleri", "Degerleme ve ekspertiz", 9, null, LegalRootId));

            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 1), "Fotograf Studyosu", "Fotograf cekimi hizmetleri", 1, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 2), "Dugun Fotografcisi", "Dugun fotograf ve video", 2, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 3), "Video Produksiyon", "Video cekim ve kurgu", 3, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 4), "Dugun Salonu", "Dugun ve davet salonu", 4, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 5), "Etkinlik Mekani", "Etkinlik ve organizasyon mekani", 5, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 6), "Dogum Gunu Organizasyonu", "Dogum gunu organizasyonu", 6, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 7), "Makyaj & Gelin Hazirligi", "Gelin ve makyaj hazirligi", 7, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 8), "Nisan & Soz Organizasyonu", "Nisan ve soz organizasyonu", 8, null, PhotoRootId));

            nodes.Add(new CategoryNode(NewSub(PetRootId, 1), "Veteriner", "Veteriner saglik hizmetleri", 1, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 2), "Pet Kuafor", "Evcil hayvan bakim ve kuafor", 2, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 3), "Pet Bakim Merkezi", "Evcil hayvan bakim merkezi", 3, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 4), "Kopek Egitim Merkezi", "Kopek egitim hizmetleri", 4, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 5), "Pet Oteli", "Evcil hayvan konaklama", 5, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 6), "Hayvan Rehabilitasyon Merkezi", "Hayvan rehabilitasyon hizmetleri", 6, null, PetRootId));

            nodes.Add(new CategoryNode(NewSub(TravelRootId, 1), "Otel", "Otel konaklama hizmetleri", 1, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 2), "Butik Otel", "Butik otel konaklama", 2, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 3), "Pansiyon", "Pansiyon konaklama", 3, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 4), "Spa Oteli", "Spa ve konaklama", 4, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 5), "Turizm Acentesi", "Tur ve seyahat acenteligi", 5, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 6), "Tur Rehberligi", "Rehberli tur hizmetleri", 6, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 7), "Tekne Turu", "Tekne turu hizmetleri", 7, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 8), "Arac Kiralama", "Arac kiralama hizmetleri", 8, null, TravelRootId));

            nodes.Add(new CategoryNode(NewSub(FashionRootId, 1), "Terzi", "Terzi ve tadilat hizmetleri", 1, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 2), "Kuru Temizleme", "Kuru temizleme hizmetleri", 2, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 3), "Gelinlik Magazasi", "Gelinlik satis ve kiralama", 3, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 4), "Abiye Magazasi", "Abiye satis ve kiralama", 4, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 5), "Kisiye Ozel Tasarim", "Kisiye ozel dikim ve tasarim", 5, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 6), "Stil Danismanligi", "Kisisel stil danismanligi", 6, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 7), "Moda Atolyesi", "Moda atolyesi hizmetleri", 7, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 8), "Ayakkabi Tamiri", "Ayakkabi tamir hizmetleri", 8, null, FashionRootId));

            nodes.Add(new CategoryNode(NewSub(OtherRootId, 1), "Noter", "Noterlik hizmetleri", 1, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 2), "Kamu Danismanlik Hizmetleri", "Kamu kurumlari danismanligi", 2, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 3), "Bireysel Danismanlik", "Bireysel danismanlik hizmetleri", 3, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 4), "Meditasyon Merkezi", "Meditasyon ve farkindalik", 4, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 5), "Fal & Astroloji Danismanligi", "Fal ve astroloji danismanligi", 5, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 6), "Sanat Terapisi", "Sanat terapisi hizmetleri", 6, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 7), "El Sanatlari Atolyesi", "El sanatlari atolyesi", 7, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 8), "Paylasimli Ofis", "Paylasimli ofis hizmetleri", 8, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 9), "Toplanti Odasi Kiralama", "Toplanti odasi kiralama", 9, null, OtherRootId));

            return nodes;
        }

        /// <summary>
        /// Alt kategori icin deterministik GUID uretir. Ana kategori basina artan
        /// sayac kullanildigi icin cakisma olmaz.
        /// </summary>
        private static Guid NewSub(Guid rootId, int order)
        {
            // Ana kategori sirasina (1..12) ve alt kategori sirasina gore tek indeks.
            var rootIndex = (int)(rootId.ToByteArray()[0] - 1);
            var index = rootIndex * 100 + order;

            return SubId(index);
        }
    }
}
