namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Iletisim izni degisikliginin yonu (dokuman Modul 16).
    /// ConsentHistory kayitlarinda kullanilir.
    /// </summary>
    public enum EnumConsentAction
    {
        /// <summary>Izin verildi.</summary>
        Granted = 0,

        /// <summary>Izin geri cekildi.</summary>
        Withdrawn = 1
    }
}
