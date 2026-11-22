using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Application.Features.Reporting.Queries.GetBusinessDashboard;
using Rezervise.Application.Features.Sales.Queries.GetDailySalesSummary;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Dashboard.Queries.GetMyBusinessDashboard
{
    /// <summary>
    /// Isletme sahibinin dashboard'u. Domain hesaplama YENIDEN yazilmaz:
    /// mevcut <see cref="GetBusinessDashboardQuery"/> ve
    /// <see cref="GetDailySalesSummaryQuery"/> IMediator ile saranilir.
    /// Sahiplik oncelikli burada dogrulanir; saranilan handler'larin kendi
    /// yetki kontrolleri de ayrica gecerlidir (derin savunma).
    /// </summary>
    public class GetMyBusinessDashboardQueryHandler
        : IRequestHandler<GetMyBusinessDashboardQuery, Result<DashboardWithSummaryDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IMediator _mediator;

        public GetMyBusinessDashboardQueryHandler(
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser,
            IMediator mediator)
        {
            _businessRepository = businessRepository;
            _currentUser = currentUser;
            _mediator = mediator;
        }

        public async Task<Result<DashboardWithSummaryDto>> Handle(
            GetMyBusinessDashboardQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<DashboardWithSummaryDto>.Failure(owned.Error!);

                // 2. Rapor gunu: istenmediyse bugun (UTC).
                var date = (request.Date ?? DateTime.UtcNow).Date;

                // 3. Mevcut dashboard sorgusu ayni parametrelerle saranilir.
                var dashboardResult = await _mediator.Send(
                    new GetBusinessDashboardQuery
                    {
                        BusinessId = request.BusinessId,
                        Date = date
                    },
                    cancellationToken);

                if (dashboardResult.IsFailure)
                    return Result<DashboardWithSummaryDto>.Failure(dashboardResult.Error!);

                // 4. Ayni gunun satis/kasa ozeti de tek kaynaktan gelir.
                var summaryResult = await _mediator.Send(
                    new GetDailySalesSummaryQuery
                    {
                        BusinessId = request.BusinessId,
                        Date = date
                    },
                    cancellationToken);

                if (summaryResult.IsFailure)
                    return Result<DashboardWithSummaryDto>.Failure(summaryResult.Error!);

                // 5. Birlesik DTO: kartlar + gunluk ozet tek yanitta.
                return Result<DashboardWithSummaryDto>.Success(new DashboardWithSummaryDto
                {
                    Dashboard = dashboardResult.Value!,
                    DailySalesSummary = summaryResult.Value!
                });
            }
            catch (Exception ex)
            {
                return Result<DashboardWithSummaryDto>.Failure(
                    Error.Internal("Dashboard getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
