namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// İletişim izni değişikliğinin yönü (doküman Modül 16).
    /// ConsentHistory kayıtlarında kullanılır.
    /// </summary>
    public enum EnumConsentAction
    {
        /// <summary>İzin verildi.</summary>
        Granted = 0,

        /// <summary>İzin geri çekildi.</summary>
        Withdrawn = 1
    }
}
