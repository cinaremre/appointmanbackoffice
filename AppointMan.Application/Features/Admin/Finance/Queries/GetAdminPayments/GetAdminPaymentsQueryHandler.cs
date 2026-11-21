using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Finance.Queries.GetAdminPayments
{
    public class GetAdminPaymentsQueryHandler
        : IRequestHandler<GetAdminPaymentsQuery, Result<AdminPagedResult<AdminPaymentDto>>>
    {
        private readonly IRepository<Payment> _paymentRepository;

        public GetAdminPaymentsQueryHandler(IRepository<Payment> paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<Result<AdminPagedResult<AdminPaymentDto>>> Handle(
            GetAdminPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var paymentMethod = request.PaymentMethod;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;
                var isRefund = request.IsRefund;

                // 1. Filtre: isletme, odeme yontemi, tarih araligi ve
                //    iade/normal ayrimi (null = hepsi).
                var payments = await _paymentRepository.FindAsync(
                    p => !p.IsDeleted
                        && (businessId == null || p.BusinessId == businessId)
                        && (paymentMethod == null || p.PaymentMethod == paymentMethod)
                        && (fromDate == null || p.PaidAt >= fromDate)
                        && (toDate == null || p.PaidAt <= toDate)
                        && (isRefund == null || p.IsRefund == isRefund),
                    cancellationToken);

                var ordered = payments.OrderByDescending(p => p.PaidAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new AdminPaymentDto
                    {
                        Id = p.Id,
                        BusinessId = p.BusinessId,
                        SaleId = p.SaleId,
                        CashSessionId = p.CashSessionId,
                        Amount = p.Amount,
                        Currency = p.Currency,
                        PaymentMethod = p.PaymentMethod,
                        PaidAt = p.PaidAt,
                        Status = p.Status,
                        IsRefund = p.IsRefund,
                        RefersToPaymentId = p.RefersToPaymentId
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminPaymentDto>>.Success(
                    new AdminPagedResult<AdminPaymentDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminPaymentDto>>.Failure(
                    Error.Internal("Odeme listesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
