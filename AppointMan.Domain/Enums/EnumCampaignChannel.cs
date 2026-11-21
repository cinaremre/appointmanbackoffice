namespace AppointMan.Domain.Enums
{
    /// <summary>
    /// Kampanyanin yurutuldugu kanal.
    /// </summary>
    public enum EnumCampaignChannel
    {
        /// <summary>Instagram organik/reklam.</summary>
        Instagram = 0,

        /// <summary>Meta (Facebook/Instagram) reklamlari.</summary>
        MetaAds = 1,

        /// <summary>Google reklamlari.</summary>
        GoogleAds = 2,

        /// <summary>Organik erisim.</summary>
        Organic = 3,

        /// <summary>Dogrudan erisim.</summary>
        Direct = 4,

        /// <summary>Diger kanallar.</summary>
        Other = 5
    }
}
