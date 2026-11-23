namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Kampanyanın yürütüldüğü kanal.
    /// </summary>
    public enum EnumCampaignChannel
    {
        /// <summary>Instagram organik/reklam.</summary>
        Instagram = 0,

        /// <summary>Meta (Facebook/Instagram) reklamları.</summary>
        MetaAds = 1,

        /// <summary>Google reklamları.</summary>
        GoogleAds = 2,

        /// <summary>Organik erişim.</summary>
        Organic = 3,

        /// <summary>Doğrudan erişim.</summary>
        Direct = 4,

        /// <summary>Diğer kanallar.</summary>
        Other = 5
    }
}
