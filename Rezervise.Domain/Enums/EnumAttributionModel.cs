namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Randevunun kampanyaya atfedilme modeli.
    /// </summary>
    public enum EnumAttributionModel
    {
        /// <summary>Son doğrudan olmayan tık (varsayılan).</summary>
        LastNonDirectClick = 0,

        /// <summary>Bilinen ilk temas.</summary>
        FirstKnownTouch = 1,

        /// <summary>Doğrudan erişim.</summary>
        Direct = 2,

        /// <summary>Organik erişim.</summary>
        Organic = 3,

        /// <summary>Ücretli kanal.</summary>
        Paid = 4,

        /// <summary>Kaynak bilinmiyor.</summary>
        Unknown = 5
    }
}
