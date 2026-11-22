using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Finance.Queries.GetMyPayments
{
    /// <summary>
    /// Isletme sahibinin odeme listesi. Sahiplik OwnerScope ile
    /// dogrulanir; iade (ters) kayitlar bayrak filtresiyle ayri
    /// listelenebilir.
    /// </summary>
    public class GetMyPaymentsQueryHandler
        : IRequestHandler<GetMyPaymentsQuery, Result<AdminPagedResult<OwnerPaymentDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyPaymentsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Payment> paymentRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _paymentRepository = paymentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerPaymentDto>>> Handle(
            GetMyPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerPaymentDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var isRefund = request.IsRefund;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                // 3. Filtre: isletme + iade bayragi + tarih araligi.
                var payments = await _paymentRepository.FindAsync(
                    p => p.BusinessId == businessId
                        && !p.IsDeleted
                        && (isRefund == null || p.IsRefund == isRefund)
                        && (fromDate == null || p.PaidAt >= fromDate)
                        && (toDate == null || p.PaidAt <= toDate),
                    cancellationToken);

                // 4. En yeni odeme once.
                var ordered = payments
                    .OrderByDescending(p => p.PaidAt)
                    .ThenBy(p => p.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new OwnerPaymentDto
                    {
                        Id = p.Id,
                        SaleId = p.SaleId,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        PaymentMethod = p.PaymentMethod,
                        PaidAt = p.PaidAt,
                        Status = p.Status,
                        IsRefund = p.IsRefund,
                        RefersToPaymentId = p.RefersToPaymentId
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerPaymentDto>>.Success(
                    new AdminPagedResult<OwnerPaymentDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerPaymentDto>>.Failure(
                    Error.Internal("Odeme listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
