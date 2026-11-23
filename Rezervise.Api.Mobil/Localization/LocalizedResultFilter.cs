using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Api projesinde Microsoft.AspNetCore.Http.IResult da bulunduğu için
// uygulama katmanındaki sonuç arayüzü alias ile ayrılır.
using AppResult = Rezervise.Application.Common.Results.IResult;

namespace Rezervise.Api.Localization
{
    /// <summary>
    /// <see cref="IResult"/> zarflarındaki lokalizasyon anahtarlarını aktif kültürde
    /// çözer. Böylece handler'lar anahtar üretir, metne çevirme tek noktada yapılır.
    /// HTTP durum kodu değiştirilmez; yalnızca zarfın mesajı lokalize edilir.
    /// </summary>
    public sealed class LocalizedResultFilter : IAlwaysRunResultFilter
    {
        private readonly ILocalizationService _localizer;

        /// <summary>
        /// Lokalizasyon servisi ile filtre oluşturur.
        /// </summary>
        /// <param name="localizer">Aktif kültürde metin cozen servis.</param>
        public LocalizedResultFilter(ILocalizationService localizer)
        {
            _localizer = localizer;
        }

        /// <inheritdoç />
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not ObjectResult objectResult)
                return;

            if (objectResult.Value is not AppResult result)
                return;

            var error = result.Error;

            // Anahtar yoksa (örneğin doğrudan metin döndüren eski çağrılar) dokunma.
            if (error is null || string.IsNullOrWhiteSpace(error.Key))
                return;

            var message = error.KeyArgs is { Length: > 0 } args
                ? _localizer.Get(error.Key, args)
                : _localizer.Get(error.Key);

            objectResult.Value = result.WithError(error.WithMessage(message));
        }

        /// <inheritdoç />
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // İşlem sonrası yapılacak bir şey yok.
        }
    }
}
