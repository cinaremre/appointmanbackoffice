using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Businesses.Queries.GetBusinessAuditTrail
{
    /// <summary>
    /// Isletme denetim kaydi handler'i (dokuman Modul 21). AdminAuditLog
    /// append-only kayitlarindan okur; isletme bulunmasa bile liste bos doner.
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
                // 1. Sayfalama kurallari diger admin listeleri ile ayni.
                var page = AdminPagination.NormalizePage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                // 2. Isletmeye ait denetim kayitlari.
                var businessId = request.BusinessId;
                var logs = await _auditLogRepository.FindAsync(
                    l => l.BusinessId == businessId, cancellationToken);

                // 3. En yeni kayit once; sayfalama bellek icinde uygulanir.
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
                    Error.Internal("Denetim kayitlari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
