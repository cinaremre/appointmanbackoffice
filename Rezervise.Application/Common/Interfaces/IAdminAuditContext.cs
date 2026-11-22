namespace Rezervise.Application.Common.Interfaces
{
    /// <summary>
    /// Backoffice istek baglamindan tasinan denetim bilgisi.
    /// Handler'lar audit kaydina correlation ve adres bilgisini buradan alir.
    /// </summary>
    public interface IAdminAuditContext
    {
        /// <summary>Log korelasyon kimligi; yoksa bos metin.</summary>
        string CorrelationId { get; }

        /// <summary>Istegi yapan istemci IP adresi; yoksa bos metin.</summary>
        string IpAddress { get; }
    }
}
