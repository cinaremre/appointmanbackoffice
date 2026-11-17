namespace AppointMan.Application.Common.Models
{
    /// <summary>
    /// Gonderilecek e-posta icerigi. Kuyruga verilen ornekler degistirilmez.
    /// </summary>
    public sealed class EmailMessage
    {
        /// <summary>Alici e-posta adresi.</summary>
        public required string To { get; init; }

        /// <summary>E-posta konusu.</summary>
        public required string Subject { get; init; }

        /// <summary>HTML govde.</summary>
        public required string HtmlBody { get; init; }

        /// <summary>Opsiyonel duz metin govde (HTML desteklemeyen istemciler icin).</summary>
        public string? TextBody { get; init; }
    }
}
