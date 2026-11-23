namespace Rezervise.Application.Common.Models
{
    /// <summary>
    /// Gönderilecek e-posta içeriği. Kuyruğa verilen örnekler değiştirilmez.
    /// </summary>
    public sealed class EmailMessage
    {
        /// <summary>Alıcı e-posta adresi.</summary>
        public required string To { get; init; }

        /// <summary>E-posta konusu.</summary>
        public required string Subject { get; init; }

        /// <summary>HTML gövde.</summary>
        public required string HtmlBody { get; init; }

        /// <summary>Opsiyonel düz metin gövde (HTML desteklemeyen istemciler için).</summary>
        public string? TextBody { get; init; }
    }
}
