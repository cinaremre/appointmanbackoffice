using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Persistence.SeedData
{
    /// <summary>
    /// İşletme kategorilerini ana/alt kategori hiyerarşisi olarak ekler veya günceller.
    /// İdempotenttir: var olan kayıt Id ile bulunup güncellenir, yoksa eklenir.
    /// Böylece mevcut işletme kayıtlarının kategori bağlantıları bozulmaz.
    /// </summary>
    public static class BusinessCategorySeed
    {
        // Mevcut sabit GUID'ler korunur; bu kayıtlar alt kategori olarak yeni
        // ana kategorilere bağlanır. Business tablosundaki FK'lar değişmez.
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
        /// Kategori hiyerarşisini seed eder. Mevcut kayıtlar silinmez; yalnızca
        /// ad/açıklama/sıralama/üst kategori bilgileri güncellenir.
        /// </summary>
        /// <param name="context">Veritabanı bağlamı.</param>
        /// <param name="logger">Log yazıcı.</param>
        /// <returns>İşlem tamamlandığında tamamlanan görev.</returns>
        public static async Task SeedAsync(AppDbContext context, ILogger logger)
        {
            var nodes = BuildNodes();

            // IgnoreQueryFilters: daha önce pasifleştirilmiş kayıtlar da güncellenir,
            // aksi halde aynı Id ile ikinci kayıt eklenmeye çalışılırdı.
            var existing = await context.BusinessCategories
                .IgnoreQueryFilters()
                .ToListAsync();

            var byId = existing.ToDictionary(c => c.Id);

            // Önce ana kategoriler, sonra alt kategoriler: parent nesnesi elde
            // olmadan SetParentCategory çağrılamaz.
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

                    // Tip de güncellenir: eski seed tüm kökleri 99 ile yazmisti ve
                    // bu deger ancak burada düzeltilebilir (ctor yalnizca ilk
                    // olusturmada çalisir).
                    category.SetCorrespondingBusinessType(node.BusinessType);
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
                "BusinessCategories hiyerarşisi seed edildi: {Root} ana, {Sub} alt kategori",
                rootCount, nodes.Count - rootCount);
        }

        private static Guid RootId(int index) => Guid.Parse($"b1{index:D6}-0000-0000-0000-000000000000");

        private static Guid SubId(int index) => Guid.Parse($"c1{index:D6}-0000-0000-0000-000000000000");

        /// <summary>Tek bir kategori düğümü.</summary>
        private sealed record CategoryNode(
            Guid Id,
            string Name,
            string? Description,
            int DisplayOrder,
            EnumBusinessType? BusinessType,
            Guid? ParentId);

        /// <summary>
        /// Ana + alt kategori listesini üretir. Mevcut 8 kategori Id'si korunur;
        /// böylece işletme bağlantıları ve correspondingBusinessType değerleri bozulmaz.
        /// </summary>
        private static List<CategoryNode> BuildNodes()
        {
            var nodes = new List<CategoryNode>();

            // ===== Ana kategoriler =====
            // Her kök kategori, istemcinin isletme tipiyle eslestirebilmesi için ayri
            // bir correspondingBusinessType döndürür. Ilk iki deger alt kategorilerdeki
            // en küçük tipten türetilir (Barber=0, Dentist=1); Spor için anlamsal
            // karsilik olarak PersonalTrainer kullanilir. Kalan köklerin enum'da sektör
            // karsiligi yoktur; bu yüzden onlara yalnizca benzersizlik saglayan yer
            // tutucu numaralar atanir (8-14, 16) ve bu isimler sektör anlami tasimaz.
            // 99 yalnizca "Diğer Hizmetler" kökünde kalir.
            nodes.Add(new CategoryNode(BeautyRootId, "Güzellik ve Kişisel Bakım", "Kuaför, berber, bakım ve wellness hizmetleri", 1, EnumBusinessType.Barber, null));
            nodes.Add(new CategoryNode(HealthRootId, "Sağlık ve Medikal", "Sağlık, teşhis ve tedavi hizmetleri", 2, EnumBusinessType.Dentist, null));
            nodes.Add(new CategoryNode(SportsRootId, "Spor ve Fitness", "Spor salonları, stüdyolar ve antrenman hizmetleri", 3, EnumBusinessType.PersonalTrainer, null));
            nodes.Add(new CategoryNode(EducationRootId, "Eğitim ve Danışmanlık", "Kurs, ders ve kariyer danışmanlığı hizmetleri", 4, EnumBusinessType.Makeup, null));
            nodes.Add(new CategoryNode(AutomotiveRootId, "Otomotiv ve Araç", "Araç bakım, onarım ve ekspertiz hizmetleri", 5, EnumBusinessType.Piercing, null));
            nodes.Add(new CategoryNode(HomeRootId, "Ev ve Teknik", "Ev, tesisat, tadilat ve temizlik hizmetleri", 6, EnumBusinessType.Physiotherapy, null));
            nodes.Add(new CategoryNode(LegalRootId, "Hukuk ve Finans", "Hukuk, muhasebe ve finansal danışmanlık hizmetleri", 7, EnumBusinessType.HealthCenter, null));
            nodes.Add(new CategoryNode(PhotoRootId, "Fotoğraf ve Organizasyon", "Fotoğraf, organizasyon ve etkinlik hizmetleri", 8, EnumBusinessType.Psychologist, null));
            nodes.Add(new CategoryNode(PetRootId, "Evcil Hayvan", "Veteriner, bakım ve eğitim hizmetleri", 9, EnumBusinessType.BeautyCenter, null));
            nodes.Add(new CategoryNode(TravelRootId, "Konaklama ve Turizm", "Konaklama, tur ve seyahat hizmetleri", 10, EnumBusinessType.CosmeticClinic, null));
            nodes.Add(new CategoryNode(FashionRootId, "Moda ve Tasarım", "Giyim, terzi ve tasarım hizmetleri", 11, EnumBusinessType.YogaStudio, null));
            nodes.Add(new CategoryNode(OtherRootId, "Diğer Hizmetler", "Diğer profesyonel ve kişisel hizmetler", 12, EnumBusinessType.Other, null));

            // ===== Alt kategoriler =====
            // Mevcut 8 kayıt: Id'ler korunur, adlar yeni listeye uyarlanır,
            // correspondingBusinessType değerleri DEGISTIRILMEZ.
            nodes.Add(new CategoryNode(HairdresserId, "Kuaför", "Kadın kuaförü ve saç hizmetleri", 1, EnumBusinessType.Hairdresser, BeautyRootId));
            nodes.Add(new CategoryNode(BarberId, "Berber", "Erkek kuaförü ve berber hizmetleri", 2, EnumBusinessType.Barber, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 1), "Güzellik Salonu", "Güzellik ve bakım hizmetleri", 3, null, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 2), "Cilt Bakım Merkezi", "Cilt bakımı ve bakım uygulamaları", 4, null, BeautyRootId));
            nodes.Add(new CategoryNode(WaxingId, "Lazer Epilasyon Merkezi", "Epilasyon ve ağda hizmetleri", 5, EnumBusinessType.Waxing, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 3), "Kalıcı Makyaj", "Kalıcı makyaj uygulamaları", 6, null, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 4), "Protez Tırnak", "Protez tırnak uygulamaları", 7, null, BeautyRootId));
            nodes.Add(new CategoryNode(NailSalonId, "Manikür & Pedikür", "Manikür, pedikür ve tırnak sanatı", 8, EnumBusinessType.NailSalon, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 5), "Kirpik & Kas Tasarımı", "Kirpik ve kas tasarımı hizmetleri", 9, null, BeautyRootId));
            nodes.Add(new CategoryNode(MassageId, "Masaj Salonu", "Terapotik masaj hizmetleri", 10, EnumBusinessType.Massage, BeautyRootId));
            nodes.Add(new CategoryNode(SpaId, "Spa & Wellness", "Wellness ve güzellik merkezi", 11, EnumBusinessType.Spa, BeautyRootId));
            nodes.Add(new CategoryNode(NewSub(BeautyRootId, 6), "Solaryum", "Solaryum hizmetleri", 12, null, BeautyRootId));
            nodes.Add(new CategoryNode(TattooId, "Dövme & Piercing Stüdyosu", "Dövme ve vücut sanatı", 13, EnumBusinessType.Tattoo, BeautyRootId));

            nodes.Add(new CategoryNode(DentistId, "Dış Hekimi", "Dış sağlığı ve tedavi hizmetleri", 1, EnumBusinessType.Dentist, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 1), "Dış Kliniği", "Dış sağlığı kliniği", 2, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 2), "Özel Hastane", "Özel hastane hizmetleri", 3, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 3), "Tip Merkezi", "Tip merkezi hizmetleri", 4, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 4), "Poliklinik", "Poliklinik hizmetleri", 5, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 5), "Dermatoloji", "Cilt hastalıkları uzmanlığı", 6, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 6), "Fizik Tedavi Merkezi", "Fizik tedavi ve rehabilitasyon", 7, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 7), "Psikiyatri", "Psikiyatri uzmanlığı", 8, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 8), "Psikolog", "Psikolojik danışmanlık", 9, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 9), "Diyetisyen", "Beslenme ve diyet danışmanlığı", 10, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 10), "Beslenme Danışmanlığı", "Beslenme danışmanlığı", 11, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 11), "Göz Hastalıkları", "Göz hastalıkları uzmanlığı", 12, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 12), "Kulak Burun Boğaz", "KBB uzmanlığı", 13, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 13), "Kadın Hastalıkları ve Doğum", "Kadın hastalıkları ve doğum", 14, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 14), "Çocuk Sağlığı", "Çocuk sağlığı ve hastalıkları", 15, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 15), "Ortopedi", "Ortopedi ve travmatoloji", 16, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 16), "Kardıyoloji", "Kalp ve damar hastalıkları", 17, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 17), "Ürölöji", "Ürölöji uzmanlığı", 18, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 18), "Nöroloji", "Nöroloji uzmanlığı", 19, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 19), "Genel Cerrahi", "Genel cerrahi hizmetleri", 20, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 20), "Laboratuvar", "Laboratuvar hizmetleri", 21, null, HealthRootId));
            nodes.Add(new CategoryNode(NewSub(HealthRootId, 21), "Görüntüleme Merkezi", "Görüntüleme ve radyoloji", 22, null, HealthRootId));

            nodes.Add(new CategoryNode(NewSub(SportsRootId, 1), "Fitness Salonu", "Fitness ve spor salonu", 1, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 2), "Kişisel Antrenör", "Kişisel antrenman hizmetleri", 2, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 3), "Pilates Stüdyosu", "Pilates dersleri", 3, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 4), "Reformer Pilates", "Reformer pilates dersleri", 4, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 5), "Yoga Stüdyosu", "Yoga dersleri", 5, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 6), "CrossFit", "CrossFit antrenmanları", 6, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 7), "Boks Salonu", "Boks antrenmanları", 7, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 8), "Kick Boks", "Kick boks antrenmanları", 8, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 9), "Yüzme Kursu", "Yüzme dersleri", 9, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 10), "Dans Stüdyosu", "Dans dersleri", 10, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 11), "Jimnastik", "Jimnastik dersleri", 11, null, SportsRootId));
            nodes.Add(new CategoryNode(NewSub(SportsRootId, 12), "Tenis Kortu", "Tenis kortu ve dersleri", 12, null, SportsRootId));

            nodes.Add(new CategoryNode(NewSub(EducationRootId, 1), "Özel Ders", "Birebir özel ders", 1, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 2), "Dil Kursu", "Yabancı dil kursları", 2, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 3), "Müzik Kursu", "Müzik ve enstrüman kursları", 3, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 4), "Dans Kursu", "Dans kursları", 4, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 5), "Sürücü Kursu", "Ehliyet ve sürücü kursu", 5, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 6), "Eğitim Danışmanlığı", "Eğitim danışmanlığı", 6, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 7), "Kariyer Danışmanlığı", "Kariyer planlama danışmanlığı", 7, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 8), "Yaşam Koçluğu", "Yaşam koşçluğu hizmetleri", 8, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 9), "Öğrenci Koçluğu", "Öğrenci koşçluğu hizmetleri", 9, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 10), "Sınav Hazırlık Merkezi", "Sınav hazırlık kursları", 10, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 11), "Bilgisayar Kursu", "Bilgisayar ve yazılım kursları", 11, null, EducationRootId));
            nodes.Add(new CategoryNode(NewSub(EducationRootId, 12), "Sanat Atölyesi", "Sanat atölyeleri", 12, null, EducationRootId));

            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 1), "Oto Servis", "Araç servis hizmetleri", 1, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 2), "Oto Ekspertiz", "Araç ekspertiz hizmetleri", 2, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 3), "Oto Yıkama", "Araç yıkama hizmetleri", 3, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 4), "Oto Kuaför", "Araç detaylı bakım", 4, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 5), "Lastik Servisi", "Lastik değişim ve bakım", 5, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 6), "Araç Bakım Merkezi", "Periyodik araç bakımı", 6, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 7), "Kaporta & Boya", "Kaporta ve boya hizmetleri", 7, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 8), "Cam Filmi", "Araç cam filmi uygulaması", 8, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 9), "Araç Detaylı Temizlik", "Detaylı araç temizliği", 9, null, AutomotiveRootId));
            nodes.Add(new CategoryNode(NewSub(AutomotiveRootId, 10), "Motosiklet Servisi", "Motosiklet servis hizmetleri", 10, null, AutomotiveRootId));

            nodes.Add(new CategoryNode(NewSub(HomeRootId, 1), "Elektrikçi", "Elektrik tesisati ve onarım", 1, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 2), "Tesisatçı", "Su tesisati ve onarım", 2, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 3), "Kombi Servisi", "Kombi bakım ve servis", 3, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 4), "Klima Servisi", "Klima bakım ve servis", 4, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 5), "Beyaz Eşya Servisi", "Beyaz eşya servis hizmetleri", 5, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 6), "Mobilya Montajı", "Mobilya montaj hizmetleri", 6, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 7), "Temizlik Hizmeti", "Ev ve ofis temizliği", 7, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 8), "Hali Yıkama", "Hali yıkama hizmetleri", 8, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 9), "İlaçlama Hizmeti", "İlaçlama hizmetleri", 9, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 10), "Peyzaj & Bahçe Bakımı", "Peyzaj ve bahçe bakımı", 10, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 11), "İç Mimarlık", "İç mimarlık hizmetleri", 11, null, HomeRootId));
            nodes.Add(new CategoryNode(NewSub(HomeRootId, 12), "Dekorasyon Danışmanlığı", "Dekorasyon danışmanlığı", 12, null, HomeRootId));

            nodes.Add(new CategoryNode(NewSub(LegalRootId, 1), "Avukatlık Bürosu", "Avukatlık ve hukuki temsil", 1, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 2), "Hukuk Danışmanlığı", "Hukuk danışmanlığı", 2, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 3), "Malı Müşavir", "Malı müşavirlik hizmetleri", 3, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 4), "Muhasebe Bürosu", "Muhasebe ve defter tutma", 4, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 5), "Finansal Danışmanlık", "Finansal planlama danışmanlığı", 5, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 6), "Sigorta Acentesi", "Sigorta acenteliği", 6, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 7), "Gayrimenkul Danışmanlığı", "Gayrimenkul danışmanlığı", 7, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 8), "Emlak Ofisi", "Emlak alım satım hizmetleri", 8, null, LegalRootId));
            nodes.Add(new CategoryNode(NewSub(LegalRootId, 9), "Ekspertiz Hizmetleri", "Değerleme ve ekspertiz", 9, null, LegalRootId));

            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 1), "Fotoğraf Stüdyosu", "Fotoğraf çekimi hizmetleri", 1, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 2), "Düğün Fotoğrafçısı", "Düğün fotoğraf ve video", 2, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 3), "Video Prodüksiyon", "Video çekim ve kurgu", 3, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 4), "Düğün Salonu", "Düğün ve davet salonu", 4, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 5), "Etkinlik Mekanı", "Etkinlik ve organizasyon mekanı", 5, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 6), "Doğum Günü Organizasyonu", "Doğum günü organizasyonu", 6, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 7), "Makyaj & Gelin Hazırlığı", "Gelin ve makyaj hazırlığı", 7, null, PhotoRootId));
            nodes.Add(new CategoryNode(NewSub(PhotoRootId, 8), "Nisan & Söz Organizasyonu", "Nisan ve söz organizasyonu", 8, null, PhotoRootId));

            nodes.Add(new CategoryNode(NewSub(PetRootId, 1), "Veteriner", "Veteriner sağlık hizmetleri", 1, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 2), "Pet Kuaför", "Evcil hayvan bakım ve kuaför", 2, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 3), "Pet Bakım Merkezi", "Evcil hayvan bakım merkezi", 3, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 4), "Köpek Eğitim Merkezi", "Köpek eğitim hizmetleri", 4, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 5), "Pet Oteli", "Evcil hayvan konaklama", 5, null, PetRootId));
            nodes.Add(new CategoryNode(NewSub(PetRootId, 6), "Hayvan Rehabilitasyon Merkezi", "Hayvan rehabilitasyon hizmetleri", 6, null, PetRootId));

            nodes.Add(new CategoryNode(NewSub(TravelRootId, 1), "Otel", "Otel konaklama hizmetleri", 1, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 2), "Butik Otel", "Butik otel konaklama", 2, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 3), "Pansiyon", "Pansiyon konaklama", 3, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 4), "Spa Oteli", "Spa ve konaklama", 4, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 5), "Turizm Acentesi", "Tur ve seyahat acenteliği", 5, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 6), "Tur Rehberliği", "Rehberli tur hizmetleri", 6, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 7), "Tekne Turu", "Tekne turu hizmetleri", 7, null, TravelRootId));
            nodes.Add(new CategoryNode(NewSub(TravelRootId, 8), "Araç Kiralama", "Araç kiralama hizmetleri", 8, null, TravelRootId));

            nodes.Add(new CategoryNode(NewSub(FashionRootId, 1), "Terzi", "Terzi ve tadilat hizmetleri", 1, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 2), "Kuru Temizleme", "Kuru temizleme hizmetleri", 2, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 3), "Gelinlik Mağazası", "Gelinlik satış ve kiralama", 3, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 4), "Abiye Mağazası", "Abiye satış ve kiralama", 4, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 5), "Kişiye Özel Tasarım", "Kişiye özel dikim ve tasarım", 5, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 6), "Stil Danışmanlığı", "Kişisel stil danışmanlığı", 6, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 7), "Moda Atölyesi", "Moda atölyesi hizmetleri", 7, null, FashionRootId));
            nodes.Add(new CategoryNode(NewSub(FashionRootId, 8), "Ayakkabı Tamiri", "Ayakkabı tamir hizmetleri", 8, null, FashionRootId));

            nodes.Add(new CategoryNode(NewSub(OtherRootId, 1), "Noter", "Noterlik hizmetleri", 1, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 2), "Kamu Danışmanlık Hizmetleri", "Kamu kurumları danışmanlığı", 2, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 3), "Bireysel Danışmanlık", "Bireysel danışmanlık hizmetleri", 3, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 4), "Meditasyon Merkezi", "Meditasyon ve farkındalık", 4, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 5), "Fal & Astroloji Danışmanlığı", "Fal ve astroloji danışmanlığı", 5, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 6), "Sanat Terapisi", "Sanat terapisi hizmetleri", 6, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 7), "El Sanatları Atölyesi", "El sanatları atölyesi", 7, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 8), "Paylaşımlı Ofis", "Paylaşımlı ofis hizmetleri", 8, null, OtherRootId));
            nodes.Add(new CategoryNode(NewSub(OtherRootId, 9), "Toplantı Odası Kiralama", "Toplantı odası kiralama", 9, null, OtherRootId));

            return nodes;
        }

        /// <summary>
        /// Alt kategori için deterministik GUID üretir. Ana kategori basına artan
        /// sayaç kullanıldığı için çakışma olmaz.
        /// </summary>
        private static Guid NewSub(Guid rootId, int order)
        {
            // Ana kategori sırasına (1..12) ve alt kategori sırasına göre tek indeks.
            var rootIndex = (int)(rootId.ToByteArray()[0] - 1);
            var index = rootIndex * 100 + order;

            return SubId(index);
        }
    }
}
