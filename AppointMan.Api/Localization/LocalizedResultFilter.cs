using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

// Api projesinde Microsoft.AspNetCore.Http.IResult da bulundugu icin
// uygulama katmanindaki sonuc arayuzu alias ile ayrilir.
using AppResult = AppointMan.Application.Common.Results.IResult;

namespace AppointMan.Api.Localization
{
    /// <summary>
    /// <see cref="IResult"/> zarflarindaki lokalizasyon anahtarlarini aktif kulturde
    /// cozer. Boylece handler'lar anahtar uretir, metne cevirme tek noktada yapilir.
    /// HTTP durum kodu degistirilmez; yalnizca zarfin mesaji lokalize edilir.
    /// </summary>
    public sealed class LocalizedResultFilter : IAlwaysRunResultFilter
    {
        private readonly ILocalizationService _localizer;

        /// <summary>
        /// Lokalizasyon servisi ile filtre olusturur.
        /// </summary>
        /// <param name="localizer">Aktif kulturde metin cozen servis.</param>
        public LocalizedResultFilter(ILocalizationService localizer)
        {
            _localizer = localizer;
        }

        /// <inheritdoc />
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is not ObjectResult objectResult)
                return;

            if (objectResult.Value is not AppResult result)
                return;

            var error = result.Error;

            // Anahtar yoksa (ornegin dogrudan metin donduren eski cagrilar) dokunma.
            if (error is null || string.IsNullOrWhiteSpace(error.Key))
                return;

            var message = error.KeyArgs is { Length: > 0 } args
                ? _localizer.Get(error.Key, args)
                : _localizer.Get(error.Key);

            objectResult.Value = result.WithError(error.WithMessage(message));
        }

        /// <inheritdoc />
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // Islem sonrasi yapilacak bir sey yok.
        }
    }
}
