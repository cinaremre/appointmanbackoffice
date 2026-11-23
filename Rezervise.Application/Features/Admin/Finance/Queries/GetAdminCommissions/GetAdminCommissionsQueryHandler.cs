using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Finance.Queries.GetAdminCommissions
{
    public class GetAdminCommissionsQueryHandler
        : IRequestHandler<GetAdminCommissionsQuery, Result<AdminPagedResult<AdminCommissionDto>>>
    {
        private readonly IRepository<CommissionAccrual> _commissionRepository;

        public GetAdminCommissionsQueryHandler(IRepository<CommissionAccrual> commissionRepository)
        {
            _commissionRepository = commissionRepository;
        }

        public async Task<Result<AdminPagedResult<AdminCommissionDto>>> Handle(
            GetAdminCommissionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var employeeId = request.EmployeeId;
                var status = request.Status;

                var commissions = await _commissionRepository.FindAsync(
                    c => !c.IsDeleted
                        && (businessId == null || c.BusinessId == businessId)
                        && (employeeId == null || c.EmployeeId == employeeId)
                        && (status == null || c.Status == status),
                    cancellationToken);

                var ordered = commissions.OrderByDescending(c => c.CalculatedAt).ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new AdminCommissionDto
                    {
                        Id = c.Id,
                        BusinessId = c.BusinessId,
                        EmployeeId = c.EmployeeId,
                        SaleLineId = c.SaleLineId,
                        RuleId = c.RuleId,
                        BaseAmount = c.BaseAmount,
                        CommissionAmount = c.CommissionAmount,
                        Status = c.Status,
                        CalculatedAt = c.CalculatedAt
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminCommissionDto>>.Success(
                    new AdminPagedResult<AdminCommissionDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminCommissionDto>>.Failure(
                    Error.Internal("Prim listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
