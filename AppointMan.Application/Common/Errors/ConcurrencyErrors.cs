using AppointMan.Application.Common.Results;

namespace AppointMan.Application.Common.Errors
{
    /// <summary>
    /// Yaris kosulu kaynakli hatalar icin ortak Result failure fabrikalari.
    /// Tum mesajlar kullaniciya gosterilecek Turkce metinlerdir.
    /// </summary>
    public static class ConcurrencyErrors
    {
        /// <summary>Ayni kaydin iki kez olusturulmasi denendiginde kullanilir.</summary>
        /// <param name="message">Kullaniciya gosterilecek mesaj.</param>
        public static Error Duplicate(string message, string? detail = null)
            => Error.Conflict(message, detail);

        /// <summary>Ayni satirin es zamanli degistirilmesi durumunda kullanilir.</summary>
        public static Error ConcurrencyConflict()
            => Error.Conflict("Kayit baska bir islem tarafindan degistirildi. Lutfen tekrar deneyin.");

        /// <summary>Ayni calisanin ayni saat araligina iki randevu denemesinde kullanilir.</summary>
        public static Error BookingConflict()
            => Error.Conflict("Bu saat araliginda zaten bir randevu var.");

        /// <summary>Ayni randevuya ikinci yorum denemesinde kullanilir.</summary>
        public static Error DuplicateReview()
            => Error.Conflict("Bu randevu icin zaten bir yorum yapilmis.");

        /// <summary>Ayni isletmenin tekrar favorilere eklenmesi denemesinde kullanilir.</summary>
        public static Error DuplicateFavorite()
            => Error.Conflict("Bu isletme zaten favorilerinizde.");

        /// <summary>Ayni kullanicinin ayni isletmede tekrar kara listeye alinmasi denemesinde kullanilir.</summary>
        public static Error DuplicateBlacklistEntry()
            => Error.Conflict("Bu kullanici zaten kara listede.");

        /// <summary>Ayni e-posta veya telefon ile kayit denemesinde kullanilir.</summary>
        public static Error DuplicateRegistration()
            => Error.Conflict("Bu e-posta veya telefon numarasi zaten kayitli.");
    }
}
