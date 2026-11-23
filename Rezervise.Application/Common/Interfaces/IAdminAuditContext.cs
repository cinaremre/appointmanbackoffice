namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Backoffice istek bağlamından taşınan denetim bilgisi.
    /// Handler'lar audit kaydına correlation ve adres bilgisini buradan alır.
    /// </summary>
    public interface IAdminAuditContext
    {
        /// <summary>Log korelasyon kimliği; yoksa boş metin.</summary>
        string CorrelationId { get; }

        /// <summary>İsteği yapan istemci IP adresi; yoksa boş metin.</summary>
        string IpAddress { get; }
    }
}
