namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Randevunun kampanyaya atfedilme modeli.
    /// </summary>
    public enum EnumAttributionModel
    {
        /// <summary>Son dogrudan olmayan tik (varsayilan).</summary>
        LastNonDirectClick = 0,

        /// <summary>Bilinen ilk temas.</summary>
        FirstKnownTouch = 1,

        /// <summary>Dogrudan erisim.</summary>
        Direct = 2,

        /// <summary>Organik erisim.</summary>
        Organic = 3,

        /// <summary>Ucretli kanal.</summary>
        Paid = 4,

        /// <summary>Kaynak bilinmiyor.</summary>
        Unknown = 5
    }
}
