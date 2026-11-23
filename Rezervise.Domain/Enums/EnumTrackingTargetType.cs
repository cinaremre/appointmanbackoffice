namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Takip linkinin yönlendirdiği hedef tipi.
    /// TargetId alani bu tipe göre yorumlanır.
    /// </summary>
    public enum EnumTrackingTargetType
    {
        /// <summary>İşletme public sayfası.</summary>
        Business = 0,

        /// <summary>Tek bir hizmet detay sayfası.</summary>
        Service = 1,

        /// <summary>Çalışan profili.</summary>
        Employee = 2,

        /// <summary>Kampanya açılış sayfası.</summary>
        Campaign = 3,

        /// <summary>Şube sayfası.</summary>
        Branch = 4
    }
}
