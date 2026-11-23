using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services.Push
{
    /// <summary>
    /// Firebase Cloud Messaging (HTTP v1) üzerinden gerçek push gönderimi.
    /// Kimlik: Firebase service account (Console'dan indirilen JSON); token
    /// yenileme ve imzalama FirebaseAdmin SDK tarafından yönetilir.
    ///
    /// UNREGISTERED dönen token'lar <see cref="PushSendResult.InvalidToken"/>
    /// olarak işaretlenir; arka plan servisi kaydı pasifleştirir.
    /// </summary>
    internal sealed class FcmPushNotificationSender : IPushNotificationSender
    {
        private readonly ILogger<FcmPushNotificationSender> _logger;

        public FcmPushNotificationSender(
            PushSettings settings,
            ILogger<FcmPushNotificationSender> logger)
        {
            _logger = logger;

            // FirebaseApp process genelinde tektir; varsa yeniden kullanılır.
            var app = FirebaseApp.DefaultInstance
                ?? FirebaseApp.Create(
                    new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(settings.ServiceAccountPath),
                        ProjectId = settings.ProjectId
                    },
                    name: null);
        }

        /// <inheritdoç />
        public async Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            var payload = new Message
            {
                Token = message.Token,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = message.Title,
                    Body = message.Body
                },
                Data = message.Data?
                    .ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance
                    .SendAsync(payload, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "FCM gönderimi başarılı. MessageId={MessageId} UserId={UserId}",
                    response,
                    message.UserId);

                return PushSendResult.Ok;
            }
            catch (FirebaseMessagingException ex)
                when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered
                      || ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
            {
                // Cihaz uygulamayı kaldırmış ya da token geçersiz: tekrar denenmez.
                _logger.LogWarning(
                    "FCM token geçersiz. UserId={UserId} Code={Code}",
                    message.UserId,
                    ex.MessagingErrorCode);

                return PushSendResult.Unregistered;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "FCM gönderimi başarısız. UserId={UserId}", message.UserId);
                return PushSendResult.Failed(ex.MessagingErrorCode?.ToString() ?? "FCM_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM çağı beklenmedik şekilde başarısız. UserId={UserId}", message.UserId);
                return PushSendResult.Failed("FCM_UNEXPECTED");
            }
        }
    }
}
