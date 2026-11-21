using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Finance.Queries.GetMyCashSessions
{
    /// <summary>
    /// Isletme sahibinin kasa oturumu listesi. Sahiplik OwnerScope ile
    /// dogrulanir; oturumlar salt okunur listelenir, acma/kapama domain
    /// komutlariyla yapilir.
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
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerCashSessionDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var status = request.Status;

                // 3. Filtre: isletme + durum.
                var sessions = await _cashSessionRepository.FindAsync(
                    s => s.BusinessId == businessId
                        && !s.IsDeleted
                        && (status == null || s.Status == status),
                    cancellationToken);

                // 4. En yeni oturum once.
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
                    Error.Internal("Kasa oturumu listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
