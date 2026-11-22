using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Commands.RetryAdminNotification
{
    /// <summary>
    /// Bildirim yeniden gonderim talebi (dokuman Modul 14, kritik islem).
    ///
    /// BASITLESTIRME (ilk surum): mevcut Notification entity'sinin guncelleme
    /// metotlari sinirlidir (baslik/mesaj/okundu disinda durum tasimaz) ve iletinin
    /// fiziksel gonderimini yapan bir bildirim saglayici entegrasyonu henuz
    /// mevcut degildir. Bu yuzden handler bildirim uzerinde HICBIR degisiklik
    /// yapmaz; yalnizca bildirimin var oldugunu dogrular, kritik islem
    /// denetim kaydini yazar (Action="NotificationRetry", OldValues=IsRead durumu)
    /// ve istemciye "kabul edildi" doner. Asil yeniden kuyruga alma ve gonderim,
    /// saglayici entegrasyonu geldiginde bu handler icinde gerceklestirilecek.
    /// </summary>
    public class RetryAdminNotificationCommandHandler
        : IRequestHandler<RetryAdminNotificationCommand, Result<RetryAdminNotificationResultDto>>
    {
        /// <summary>Kabul notu: asil gonderim saglayici entegrasyonuna baglidir.</summary>
        private const string AcceptedNote =
            "Yeniden gonderim talebi kabul edildi. Bildirim saglayici entegrasyonu henuz olmadigi icin ilk surumde iletinin tekrar fiziksel olarak gonderilmez; talep yalnizca denetim kaydina yazilir.";

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
                // 1. Bildirim var mi? Varlik dogrulamasi yeniden gonderim icin on kosuldur.
                var notification = await _notificationRepository.GetByIdAsync(
                    request.NotificationId, cancellationToken);

                if (notification is null)
                    return Result<RetryAdminNotificationResultDto>.Failure(
                        Error.NotFoundById("Bildirim", request.NotificationId));

                // 2. Aktor kimligi controller tarafindan JWT'den doldurulur;
                //    cozulemediyse islem denetimsiz yapilamaz.
                if (request.ActorUserId == Guid.Empty)
                    return Result<RetryAdminNotificationResultDto>.Failure(
                        Error.Unauthorized("Yeniden gonderimi isteyen yoneticinin kimligi cozulemedi."));

                // 3. Kritik islem denetim kaydi: OldValues olarak guncel IsRead
                //    durumu yazilir. Entity degismedigi icin NewValues kabul bilgisidir.
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

                // 4. Ilk surum: entity dokunulmaz, istemciye kabul donulur.
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
                    Error.Internal("Bildirim yeniden gonderim talebi islenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
