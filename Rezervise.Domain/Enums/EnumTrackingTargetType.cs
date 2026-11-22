namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Takip linkinin yonlendirdigi hedef tipi.
    /// TargetId alani bu tipe gore yorumlanir.
    /// </summary>
    public enum EnumTrackingTargetType
    {
        /// <summary>Isletme public sayfasi.</summary>
        Business = 0,

        /// <summary>Tek bir hizmet detay sayfasi.</summary>
        Service = 1,

        /// <summary>Calisan profili.</summary>
        Employee = 2,

        /// <summary>Kampanya acilis sayfasi.</summary>
        Campaign = 3,

        /// <summary>Sube sayfasi.</summary>
        Branch = 4
    }
}
