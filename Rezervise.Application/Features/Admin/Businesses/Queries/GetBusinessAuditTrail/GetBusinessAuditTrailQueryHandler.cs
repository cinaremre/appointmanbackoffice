using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail
{
    /// <summary>
    /// İşletme denetim kaydı handler'i (doküman Modül 21). AdminAuditLog
    /// append-only kayıtlarından okur; işletme bulunmasa bile liste boş döner.
    /// </summary>
    public class GetBusinessAuditTrailQueryHandler : IRequestHandler<GetBusinessAuditTrailQuery, Result<AdminAuditTrailResult>>
    {
        private readonly IRepository<AdminAuditLog> _auditLogRepository;

        public GetBusinessAuditTrailQueryHandler(IRepository<AdminAuditLog> auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<Result<AdminAuditTrailResult>> Handle(GetBusinessAuditTrailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sayfalama kuralları diğer admin listeleri ile aynı.
                var page = AdminPagination.NormalizePage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                // 2. İşletmeye ait denetim kayıtları.
                var businessId = request.BusinessId;
                var logs = await _auditLogRepository.FindAsync(
                    l => l.BusinessId == businessId, cancellationToken);

                // 3. En yeni kayıt önce; sayfalama bellek içinde uygulanır.
                var ordered = logs.OrderByDescending(l => l.CreatedAtUtc).ToList();

                var totalCount = ordered.Count;
                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new AdminAuditTrailItemDto
                    {
                        ActorUserId = l.ActorUserId,
                        ActorRole = l.ActorRole,
                        Action = l.Action,
                        Reason = l.Reason,
                        CreatedAtUtc = l.CreatedAtUtc
                    })
                    .ToList();

                return Result<AdminAuditTrailResult>.Success(new AdminAuditTrailResult
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                });
            }
            catch (Exception ex)
            {
                return Result<AdminAuditTrailResult>.Failure(
                    Error.Internal("Denetim kayıtları getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
