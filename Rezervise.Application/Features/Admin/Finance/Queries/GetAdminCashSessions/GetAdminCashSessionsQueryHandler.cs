using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCashSessions
{
    public class GetAdminCashSessionsQueryHandler
        : IRequestHandler<GetAdminCashSessionsQuery, Result<AdminPagedResult<AdminCashSessionDto>>>
    {
        private readonly IRepository<CashSession> _cashSessionRepository;

        public GetAdminCashSessionsQueryHandler(IRepository<CashSession> cashSessionRepository)
        {
            _cashSessionRepository = cashSessionRepository;
        }

        public async Task<Result<AdminPagedResult<AdminCashSessionDto>>> Handle(
            GetAdminCashSessionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var status = request.Status;

                var sessions = await _cashSessionRepository.FindAsync(
                    s => !s.IsDeleted
                        && (businessId == null || s.BusinessId == businessId)
                        && (status == null || s.Status == status),
                    cancellationToken);

                var ordered = sessions.OrderByDescending(s => s.OpenedAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new AdminCashSessionDto
                    {
                        Id = s.Id,
                        BusinessId = s.BusinessId,
                        CashRegisterId = s.CashRegisterId,
                        OpenedBy = s.OpenedBy,
                        OpenedAt = s.OpenedAt,
                        OpeningCash = s.OpeningCash,
                        ClosedBy = s.ClosedBy,
                        ClosedAt = s.ClosedAt,
                        ExpectedCash = s.ExpectedCash,
                        CountedCash = s.CountedCash,
                        Difference = s.Difference,
                        Status = s.Status
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminCashSessionDto>>.Success(
                    new AdminPagedResult<AdminCashSessionDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminCashSessionDto>>.Failure(
                    Error.Internal("Kasa oturumları getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
