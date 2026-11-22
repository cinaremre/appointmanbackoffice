namespace Rezervise.Domain.Enums.Extensions
{
    public static class EnumServiceCategoryExtensions
    {
        /// <summary>
        /// Kategori grubunu döndürür (10, 11, 12, ... 19)
        /// </summary>
        public static int GetGroup(this EnumServiceCategory category)
        {
            var value = (int)category;

            if (value >= 1000 && value < 1100) return 10; // Berber & Kuaför
            if (value >= 1100 && value < 1200) return 11; // Dis
            if (value >= 1200 && value < 1300) return 12; // Dövme
            if (value >= 1300 && value < 1400) return 13; // Cilt
            if (value >= 1400 && value < 1500) return 14; // Masaj
            if (value >= 1500 && value < 1600) return 15; // Tirnak
            if (value >= 1600 && value < 1700) return 16; // Makyaj
            if (value >= 1700 && value < 1800) return 17; // Özel
            if (value >= 1800 && value < 1900) return 18; // Saglik
            if (value >= 1900 && value < 2000) return 19; // Diger

            return 0; // Tanimsiz
        }

        /// <summary>
        /// Grubun adini döndürür
        /// </summary>
        public static string GetGroupName(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => "Berber & Kuaför",
                11 => "Dis Hekimligi",
                12 => "Dövme & Vücut Sanati",
                13 => "Cilt & Güzellik",
                14 => "Masaj & Spa",
                15 => "Tirnak & El Bakimi",
                16 => "Makyaj & Kas",
                17 => "Özel Hizmetler",
                18 => "Saglik & Terapi",
                19 => "Diger",
                _ => "Tanimsiz"
            };
        }

        /// <summary>
        /// Belirli bir gruptaki tüm kategorileri getirir
        /// </summary>
        public static IEnumerable<EnumServiceCategory> GetCategoriesByGroup(int group)
        {
            int min = group * 100;
            int max = min + 99;

            return Enum.GetValues<EnumServiceCategory>()
                .Where(c => (int)c >= min && (int)c <= max);
        }

        /// <summary>
        /// Belirli bir BusinessType'a göre kategorileri filtreler
        /// </summary>
        public static IEnumerable<EnumServiceCategory> GetCategoriesByBusinessType(EnumBusinessType businessType)
        {
            return businessType switch
            {
                EnumBusinessType.Barber => GetCategoriesByGroup(10),
                EnumBusinessType.Hairdresser => GetCategoriesByGroup(10),
                EnumBusinessType.Dentist => GetCategoriesByGroup(11),
                EnumBusinessType.Tattoo => GetCategoriesByGroup(12),
                EnumBusinessType.Piercing => GetCategoriesByGroup(12),
                EnumBusinessType.Spa => GetCategoriesByGroup(13).Concat(GetCategoriesByGroup(14)),
                EnumBusinessType.Massage => GetCategoriesByGroup(14),
                EnumBusinessType.NailSalon => GetCategoriesByGroup(15),
                EnumBusinessType.Makeup => GetCategoriesByGroup(16),
                EnumBusinessType.Waxing => GetCategoriesByGroup(17),
                _ => GetCategoriesByGroup(19)
            };
        }

        /// <summary>
        /// Kategorinin hangi BusinessType'a ait oldugunu döndürür
        /// </summary>
        public static EnumBusinessType? GetBusinessType(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => EnumBusinessType.Barber,
                11 => EnumBusinessType.Dentist,
                12 => EnumBusinessType.Tattoo,
                13 => EnumBusinessType.Spa,
                14 => EnumBusinessType.Massage,
                15 => EnumBusinessType.NailSalon,
                16 => EnumBusinessType.Makeup,
                17 => EnumBusinessType.Waxing,
                18 => null, // Saglik servisleri herhangi bir tipe bagli degil
                19 => null, // Diger
                _ => null
            };
        }

        /// <summary>
        /// Gruba göre ikon emojisi döndürür (UI için)
        /// </summary>
        public static string GetGroupIcon(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => "??",  // Berber
                11 => "??",  // Dis
                12 => "??",  // Dövme
                13 => "?",  // Cilt
                14 => "??",  // Masaj
                15 => "??",  // Tirnak
                16 => "??",  // Makyaj
                17 => "??",  // Özel
                18 => "??",  // Saglik
                19 => "??",  // Diger
                _ => "?"
            };
        }

        /// <summary>
        /// Gruba göre renk kodu döndürür (UI için)
        /// </summary>
        public static string GetGroupColor(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => "#4CAF50", // Yesil
                11 => "#2196F3", // Mavi
                12 => "#9C27B0", // Mor
                13 => "#FF9800", // Turuncu
                14 => "#E91E63", // Pembe
                15 => "#FF5722", // Kirmizi-turuncu
                16 => "#FF6F00", // Altin
                17 => "#00BCD4", // Turkuaz
                18 => "#F44336", // Kirmizi
                19 => "#9E9E9E", // Gri
                _ => "#000000"
            };
        }

        /// <summary>
        /// Bir sonraki bos kategori numarasini bulur (yeni kategori eklemek için)
        /// </summary>
        public static int GetNextAvailableNumber(int group)
        {
            var existing = GetCategoriesByGroup(group).Select(c => (int)c).ToList();

            int start = group * 100;
            int end = start + 99;

            for (int i = start; i <= end; i++)
            {
                if (!existing.Contains(i))
                    return i;
            }

            throw new InvalidOperationException($"Grup {group} için bos alan kalmadi!");
        }
    }
}