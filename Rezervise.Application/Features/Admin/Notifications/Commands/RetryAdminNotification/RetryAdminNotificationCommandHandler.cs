using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Bildirim yeniden gönderim talebi (doküman Modül 14, kritik işlem).
    ///
    /// BASITLESTIRME (ilk sürüm): mevcut Notification entity'sinin güncelleme
    /// metotları sınırlıdır (başlık/mesaj/okundu dışında durum taşımaz) ve iletinin
    /// fiziksel gönderimini yapan bir bildirim sağlayıcı entegrasyonu henüz
    /// mevcut değildir. Bu yüzden handler bildirim üzerinde HICBIR değişiklik
    /// yapmaz; yalnızca bildirimin var öldüğünü doğrular, kritik işlem
    /// denetim kaydını yazar (Action="NotificationRetry", OldValues=IsRead durumu)
    /// ve istemciye "kabul edildi" döner. Asıl yeniden kuyruğa alma ve gönderim,
    /// sağlayıcı entegrasyonu geldiğinde bu handler içinde gerçekleştirilecek.
    /// </summary>
    public class RetryAdminNotificationCommandHandler
        : IRequestHandler<RetryAdminNotificationCommand, Result<RetryAdminNotificationResultDto>>
    {
        /// <summary>Kabul notu: asıl gönderim sağlayıcı entegrasyonuna bağlıdır.</summary>
        private const string AcceptedNote =
            "Yeniden gönderim talebi kabul edildi. Bildirim sağlayıcı entegrasyonu henüz olmadığı için ilk sürümde iletinin tekrar fiziksel olarak gönderilmez; talep yalnızca denetim kaydına yazılır.";

        private readonly IRepository<Notification> _notificationRepository;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public RetryAdminNotificationCommandHandler(
            IRepository<Notification> notificationRepository,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _notificationRepository = notificationRepository;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<RetryAdminNotificationResultDto>> Handle(
            RetryAdminNotificationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirim var mi? Varlık doğrulaması yeniden gönderim için on koşuldur.
                var notification = await _notificationRepository.GetByIdAsync(
                    request.NotificationId, cancellationToken);

                if (notification is null)
                    return Result<RetryAdminNotificationResultDto>.Failure(
                        Error.NotFoundById("Bildirim", request.NotificationId));

                // 2. Aktör kimliği controller tarafından JWT'den doldurulur;
                //    çözülemediyse işlem denetimsiz yapılamaz.
                if (request.ActorUserId == Guid.Empty)
                    return Result<RetryAdminNotificationResultDto>.Failure(
                        Error.Unauthorized("Yeniden gönderimi isteyen yöneticinin kimliği çözülemedi."));

                // 3. Kritik işlem denetim kaydı: OldValues olarak güncel IsRead
                //    durumu yazılır. Entity değişmediği için NewValues kabul bilgisidir.
                await _auditSink.RecordAsync(
                    request.ActorUserId,
                    string.IsNullOrWhiteSpace(request.ActorRole) ? "Unknown" : request.ActorRole,
                    businessId: null,
                    entityType: "Notification",
                    entityId: notification.Id.ToString(),
                    action: "NotificationRetry",
                    oldValues: $"{{\"IsRead\":{(notification.IsRead ? "true" : "false")}}}",
                    newValues: "{\"Retry\":\"Accepted\"}",
                    reason: request.Reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                // 4. İlk sürüm: entity dokunulmaz, istemciye kabul dönülür.
                return Result<RetryAdminNotificationResultDto>.Success(
                    new RetryAdminNotificationResultDto
                    {
                        NotificationId = notification.Id,
                        UserId = notification.UserId,
                        Type = notification.Type,
                        Note = AcceptedNote
                    });
            }
            catch (Exception ex)
            {
                return Result<RetryAdminNotificationResultDto>.Failure(
                    Error.Internal("Bildirim yeniden gönderim talebi işlenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
