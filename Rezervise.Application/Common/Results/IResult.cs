namespace Rezervise.Application.Common.Results
{
    public interface IResult
    {
        bool IsSuccess { get; }
        bool IsFailure { get; }
        Error? Error { get; }

        /// <summary>
        /// Hatası değiştirilmiş yeni bir sonuç üretir. Api katmanı lokalize
        /// edilmiş mesajı bu yöntemle zarfa geri yazar.
        /// </summary>
        /// <param name="error">Yeni hata bilgisi.</param>
        /// <returns>Hatası değiştirilmiş sonuç.</returns>
        IResult WithError(Error error);
    }
}
