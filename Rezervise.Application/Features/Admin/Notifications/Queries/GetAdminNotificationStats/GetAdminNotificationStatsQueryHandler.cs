using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Notifications.Queries.GetAdminNotificationStats
{
    /// <summary>
    /// Son 30 gunun bildirimlerini tek sorguda ceker, okunma ve tip kirilimlarini
    /// bellekte hesaplar. Saglayici bazli gonderim maliyeti raporu (SMS/e-posta)
    /// bildirim saglayici entegrasyonu sonrasinda eklenecektir; ilk surum
    /// yalnizca uygulama ici bildirimleri sayar.
    /// </summary>
    public class GetAdminNotificationStatsQueryHandler
        : IRequestHandler<GetAdminNotificationStatsQuery, Result<AdminNotificationStatsDto>>
    {
        /// <summary>Istatistik penceresi uzunlugu.</summary>
        private const int WindowDays = 30;

        /// <summary>Saglayici entegrasyonu sonrasina kadar maliyet kirilimi yok.</summary>
        private const string CostNoteText =
            "Saglayici bazli gonderim maliyeti raporu bildirim saglayici (SMS/e-posta) entegrasyonu sonrasinda eklenecek; bu surumde yalnizca uygulama ici bildirimler sayilir.";

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
                    Error.Internal("Bildirim istatistikleri hesaplanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
