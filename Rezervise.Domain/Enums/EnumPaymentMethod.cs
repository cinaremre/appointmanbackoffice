namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Ödeme yöntemleri
    /// </summary>
    public enum EnumPaymentMethod
    {
        /// <summary>Nakit</summary>
        Cash = 0,

        /// <summary>Banka kartı (POS)</summary>
        BankCard = 1,

        /// <summary>Havale / EFT</summary>
        Transfer = 2,

        /// <summary>Online ödeme (sanal POS, cüzdan)</summary>
        Online = 3,

        /// <summary>Hediye çeki</summary>
        GiftCard = 4,

        /// <summary>Paket kredisi / abonman</summary>
        PackageCredit = 5
    }
}
