using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 günün bildirimlerini tek sorguda çeker, okunma ve tip kırılımlarını
    /// bellekte hesaplar. Sağlayıcı bazlı gönderim maliyeti raporu (SMS/e-posta)
    /// bildirim sağlayıcı entegrasyonu sonrasında eklenecektir; ilk sürüm
    /// yalnızca uygulama içi bildirimleri sayar.
    /// </summary>
    public class GetAdminNotificationStatsQueryHandler
        : IRequestHandler<GetAdminNotificationStatsQuery, Result<AdminNotificationStatsDto>>
    {
        /// <summary>İstatistik penceresi uzunluğu.</summary>
        private const int WindowDays = 30;

        /// <summary>Sağlayıcı entegrasyonu sonrasına kadar maliyet kırılımı yok.</summary>
        private const string CostNoteText =
            "Sağlayıcı bazlı gönderim maliyeti raporu bildirim sağlayıcı (SMS/e-posta) entegrasyonu sonrasında eklenecek; bu sürümde yalnızca uygulama içi bildirimler sayılır.";

        private readonly IRepository<Notification> _notificationRepository;

        public GetAdminNotificationStatsQueryHandler(IRepository<Notification> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Result<AdminNotificationStatsDto>> Handle(
            GetAdminNotificationStatsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var toDate = DateTime.UtcNow;
                var fromDate = toDate.AddDays(-WindowDays);

                var notifications = await _notificationRepository.FindAsync(
                    n => !n.IsDeleted && n.SentAt >= fromDate && n.SentAt <= toDate,
                    cancellationToken);

                var dto = new AdminNotificationStatsDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Total = notifications.Count,
                    ReadCount = notifications.Count(n => n.IsRead),
                    UnreadCount = notifications.Count(n => !n.IsRead),
                    CostNote = CostNoteText,
                    ByType = notifications
                        .GroupBy(n => n.Type)
                        .Select(g => new AdminNotificationTypeStatDto
                        {
                            Type = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(t => t.Count)
                        .ThenBy(t => t.Type)
                        .ToList()
                };

                return Result<AdminNotificationStatsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminNotificationStatsDto>.Failure(
                    Error.Internal("Bildirim istatistikleri hesaplanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
