using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Audit.Queries.GetAdminAuditLogs
{
    /// <summary>
    /// Denetim kayitlarini filtreler ve CreatedAtUtc tersten siralar.
    /// Kayitlar append-only'dir; sorgu yalnizca okuma yapar.
    /// </summary>
    public class GetAdminAuditLogsQueryHandler
        : IRequestHandler<GetAdminAuditLogsQuery, Result<AdminAuditLogListDto>>
    {
        private readonly IRepository<AdminAuditLog> _auditRepository;

        public GetAdminAuditLogsQueryHandler(IRepository<AdminAuditLog> auditRepository)
        {
            _auditRepository = auditRepository;
        }

        public async Task<Result<AdminAuditLogListDto>> Handle(
            GetAdminAuditLogsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var actorUserId = request.ActorUserId;
                var businessId = request.BusinessId;
                var entityType = string.IsNullOrWhiteSpace(request.EntityType)
                    ? null
                    : request.EntityType.Trim();
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                var logs = await _auditRepository.FindAsync(
                    l => !l.IsDeleted
                        && (actorUserId == null || l.ActorUserId == actorUserId)
                        && (businessId == null || l.BusinessId == businessId)
                        && (entityType == null || l.EntityType == entityType)
                        && (fromDate == null || l.CreatedAtUtc >= fromDate)
                        && (toDate == null || l.CreatedAtUtc <= toDate),
                    cancellationToken);

                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var ordered = logs
                    .OrderByDescending(l => l.CreatedAtUtc)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new AdminAuditLogDto
                    {
                        Id = l.Id,
                        ActorUserId = l.ActorUserId,
                        ActorRole = l.ActorRole,
                        BusinessId = l.BusinessId,
                        EntityType = l.EntityType,
                        EntityId = l.EntityId,
                        Action = l.Action,
                        OldValues = l.OldValues,
                        NewValues = l.NewValues,
                        Reason = l.Reason,
                        CorrelationId = l.CorrelationId,
                        IpAddress = l.IpAddress,
                        CreatedAtUtc = l.CreatedAtUtc
                    })
                    .ToList();

                return Result<AdminAuditLogListDto>.Success(new AdminAuditLogListDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result<AdminAuditLogListDto>.Failure(
                    Error.Internal("Denetim kayitlari getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
