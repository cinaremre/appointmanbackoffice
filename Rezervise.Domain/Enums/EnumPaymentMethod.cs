namespace Rezervise.Domain.Enums
{
    /// <summary>
    /// Odeme yontemleri
    /// </summary>
    public enum EnumPaymentMethod
    {
        /// <summary>Nakit</summary>
        Cash = 0,

        /// <summary>Banka karti (POS)</summary>
        BankCard = 1,

        /// <summary>Havale / EFT</summary>
        Transfer = 2,

        /// <summary>Online odeme (sanal POS, cuzdan)</summary>
        Online = 3,

        /// <summary>Hediye ceki</summary>
        GiftCard = 4,

        /// <summary>Paket kredisi / abonman</summary>
        PackageCredit = 5
    }
}
