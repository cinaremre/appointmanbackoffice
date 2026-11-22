namespace Rezervise.Application.Common.Results
{
    public interface IResult
    {
        bool IsSuccess { get; }
        bool IsFailure { get; }
        Error? Error { get; }

        /// <summary>
        /// Hatasi degistirilmis yeni bir sonuc uretir. Api katmani lokalize
        /// edilmis mesaji bu yontemle zarfa geri yazar.
        /// </summary>
        /// <param name="error">Yeni hata bilgisi.</param>
        /// <returns>Hatasi degistirilmis sonuc.</returns>
        IResult WithError(Error error);
    }
}
