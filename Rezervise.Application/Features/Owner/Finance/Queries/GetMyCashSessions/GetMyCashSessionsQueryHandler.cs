using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMyCashSessions
{
    /// <summary>
    /// İşletme sahibinin kasa oturumu listesi. Sahiplik OwnerScope ile
    /// doğrulanır; oturumlar salt okunur listelenir, açma/kapama domain
    /// komutlarıyla yapılır.
    /// </summary>
    public class GetMyCashSessionsQueryHandler
        : IRequestHandler<GetMyCashSessionsQuery, Result<AdminPagedResult<OwnerCashSessionDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyCashSessionsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<CashSession> cashSessionRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _cashSessionRepository = cashSessionRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerCashSessionDto>>> Handle(
            GetMyCashSessionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerCashSessionDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var status = request.Status;

                // 3. Filtre: işletme + durum.
                var sessions = await _cashSessionRepository.FindAsync(
                    s => s.BusinessId == businessId
                        && !s.IsDeleted
                        && (status == null || s.Status == status),
                    cancellationToken);

                // 4. En yeni oturum önce.
                var ordered = sessions
                    .OrderByDescending(s => s.OpenedAt)
                    .ThenBy(s => s.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new OwnerCashSessionDto
                    {
                        Id = s.Id,
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

                return Result<AdminPagedResult<OwnerCashSessionDto>>.Success(
                    new AdminPagedResult<OwnerCashSessionDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerCashSessionDto>>.Failure(
                    Error.Internal("Kasa oturumu listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
