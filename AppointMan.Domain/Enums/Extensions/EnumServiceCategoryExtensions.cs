namespace AppointMan.Domain.Enums.Extensions
{
    internal static class EnumServiceCategoryExtensions
    {
        /// <summary>
        /// Kategori grubunu döndürür (10, 11, 12, ... 19)
        /// </summary>
        public static int GetGroup(this EnumServiceCategory category)
        {
            var value = (int)category;

            if (value >= 1000 && value < 1100) return 10; // Berber & Kuaför
            if (value >= 1100 && value < 1200) return 11; // Diş
            if (value >= 1200 && value < 1300) return 12; // Dövme
            if (value >= 1300 && value < 1400) return 13; // Cilt
            if (value >= 1400 && value < 1500) return 14; // Masaj
            if (value >= 1500 && value < 1600) return 15; // Tırnak
            if (value >= 1600 && value < 1700) return 16; // Makyaj
            if (value >= 1700 && value < 1800) return 17; // Özel
            if (value >= 1800 && value < 1900) return 18; // Sağlık
            if (value >= 1900 && value < 2000) return 19; // Diğer

            return 0; // Tanımsız
        }

        /// <summary>
        /// Grubun adını döndürür
        /// </summary>
        public static string GetGroupName(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => "Berber & Kuaför",
                11 => "Diş Hekimliği",
                12 => "Dövme & Vücut Sanatı",
                13 => "Cilt & Güzellik",
                14 => "Masaj & Spa",
                15 => "Tırnak & El Bakımı",
                16 => "Makyaj & Kaş",
                17 => "Özel Hizmetler",
                18 => "Sağlık & Terapi",
                19 => "Diğer",
                _ => "Tanımsız"
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
        /// Kategorinin hangi BusinessType'a ait olduğunu döndürür
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
                18 => null, // Sağlık servisleri herhangi bir tipe bağlı değil
                19 => null, // Diğer
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
                10 => "✂️",  // Berber
                11 => "🦷",  // Diş
                12 => "💉",  // Dövme
                13 => "✨",  // Cilt
                14 => "💆",  // Masaj
                15 => "💅",  // Tırnak
                16 => "💄",  // Makyaj
                17 => "🌸",  // Özel
                18 => "❤️",  // Sağlık
                19 => "📌",  // Diğer
                _ => "❓"
            };
        }

        /// <summary>
        /// Gruba göre renk kodu döndürür (UI için)
        /// </summary>
        public static string GetGroupColor(this EnumServiceCategory category)
        {
            return category.GetGroup() switch
            {
                10 => "#4CAF50", // Yeşil
                11 => "#2196F3", // Mavi
                12 => "#9C27B0", // Mor
                13 => "#FF9800", // Turuncu
                14 => "#E91E63", // Pembe
                15 => "#FF5722", // Kırmızı-turuncu
                16 => "#FF6F00", // Altın
                17 => "#00BCD4", // Turkuaz
                18 => "#F44336", // Kırmızı
                19 => "#9E9E9E", // Gri
                _ => "#000000"
            };
        }

        /// <summary>
        /// Bir sonraki boş kategori numarasını bulur (yeni kategori eklemek için)
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

            throw new InvalidOperationException($"Grup {group} için boş alan kalmadı!");
        }
    }
}