using Rezervise.Application.Common.Results;

namespace Rezervise.Application.Common.Errors
{
    /// <summary>
    /// Yarış koşulu kaynaklı hatalar için ortak Result failure fabrikaları.
    /// Tüm mesajlar kullanıcıya gösterilecek Türkçe metinlerdir.
    /// </summary>
    public static class ConcurrencyErrors
    {
        /// <summary>Aynı kaydın iki kez oluşturulması denendiğinde kullanılır.</summary>
        /// <param name="message">Kullanıcıya gösterilecek mesaj.</param>
        public static Error Duplicate(string message, string? detail = null)
            => Error.Conflict(message, detail);

        /// <summary>Aynı satırın es zamanlı değiştirilmesi durumunda kullanılır.</summary>
        public static Error ConcurrencyConflict()
            => Error.Conflict("Kayıt başka bir işlem tarafından değiştirildi. Lütfen tekrar deneyin.");

        /// <summary>Aynı çalışanın aynı saat aralığına iki randevu denemesinde kullanılır.</summary>
        public static Error BookingConflict()
            => Error.Conflict("Bu saat aralığında zaten bir randevu var.");

        /// <summary>Aynı randevuya ikinci yorum denemesinde kullanılır.</summary>
        public static Error DuplicateReview()
            => Error.Conflict("Bu randevu için zaten bir yorum yapılmış.");

        /// <summary>Aynı işletmenin tekrar favorilere eklenmesi denemesinde kullanılır.</summary>
        public static Error DuplicateFavorite()
            => Error.Conflict("Bu işletme zaten favorilerinizde.");

        /// <summary>Aynı kullanıcının aynı işletmede tekrar kara listeye alınması denemesinde kullanılır.</summary>
        public static Error DuplicateBlacklistEntry()
            => Error.Conflict("Bu kullanıcı zaten kara listede.");

        /// <summary>Aynı e-posta veya telefon ile kayıt denemesinde kullanılır.</summary>
        public static Error DuplicateRegistration()
            => Error.Conflict("Bu e-posta veya telefon numarası zaten kayıtlı.");
    }
}
