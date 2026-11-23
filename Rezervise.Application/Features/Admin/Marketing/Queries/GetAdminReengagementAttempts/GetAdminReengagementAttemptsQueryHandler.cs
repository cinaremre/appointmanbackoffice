using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Marketing.Queries.GetAdminReengagementAttempts
{
    /// <summary>
    /// Geri çağırma denemelerini filtreler ve tetiklenme zamanına göre tersten
    /// sıralar. Liste sorgusu öldüğü için sayfalama uygulanmaz; dönüşüm takibi
    /// için rapor kullanımına uygun boyutta tutulur.
    /// </summary>
    public class GetAdminReengagementAttemptsQueryHandler
        : IRequestHandler<GetAdminReengagementAttemptsQuery, Result<AdminReengagementAttemptListDto>>
    {
        private readonly IRepository<ReengagementAttempt> _attemptRepository;

        public GetAdminReengagementAttemptsQueryHandler(IRepository<ReengagementAttempt> attemptRepository)
        {
            _attemptRepository = attemptRepository;
        }

        public async Task<Result<AdminReengagementAttemptListDto>> Handle(
            GetAdminReengagementAttemptsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var businessId = request.BusinessId;
                var status = request.Status;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var attempts = await _attemptRepository.FindAsync(
                    a => !a.IsDeleted
                        && (businessId == null || a.BusinessId == businessId)
                        && (status == null || a.Status == status)
                        && (fromDate == null || a.TriggeredAt >= fromDate)
                        && (toDate == null || a.TriggeredAt <= toDate),
                    cancellationToken);

                var items = attempts
                    .OrderByDescending(a => a.TriggeredAt)
                    .Select(a => new AdminReengagementAttemptDto
                    {
                        Id = a.Id,
                        BusinessId = a.BusinessId,
                        CustomerId = a.CustomerId,
                        ServiceId = a.ServiceId,
                        RuleId = a.RuleId,
                        NotificationId = a.NotificationId,
                        TriggeredAt = a.TriggeredAt,
                        Status = a.Status,
                        ConvertedAppointmentId = a.ConvertedAppointmentId
                    })
                    .ToList();

                return Result<AdminReengagementAttemptListDto>.Success(
                    new AdminReengagementAttemptListDto { Items = items });
            }
            catch (Exception ex)
            {
                return Result<AdminReengagementAttemptListDto>.Failure(
                    Error.Internal("Geri çağırma denemeleri getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
