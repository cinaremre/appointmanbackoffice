using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Stock.Queries.GetAdminCustomerPackages
{
    public class GetAdminCustomerPackagesQueryHandler
        : IRequestHandler<GetAdminCustomerPackagesQuery, Result<AdminPagedResult<AdminCustomerPackageDto>>>
    {
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<PackageUsage> _packageUsageRepository;

        public GetAdminCustomerPackagesQueryHandler(
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<PackageUsage> packageUsageRepository)
        {
            _customerPackageRepository = customerPackageRepository;
            _servicePackageRepository = servicePackageRepository;
            _packageUsageRepository = packageUsageRepository;
        }

        public async Task<Result<AdminPagedResult<AdminCustomerPackageDto>>> Handle(
            GetAdminCustomerPackagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var customerId = request.CustomerId;
                var status = request.Status;

                // 1. Paket örnekleri filtresi: işletme, müşteri ve durum.
                var packages = await _customerPackageRepository.FindAsync(
                    p => !p.IsDeleted
                        && (businessId == null || p.BusinessId == businessId)
                        && (customerId == null || p.CustomerId == customerId)
                        && (status == null || p.Status == status),
                    cancellationToken);

                var ordered = packages.OrderByDescending(p => p.PurchasedAt).ToList();

                var pageItems = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 2. Purchased: paket tanımındaki toplam seans sayısı
                //    (ServicePackageItem toplamı).
                var servicePackageIds = pageItems
                    .Select(p => p.ServicePackageId)
                    .Distinct()
                    .ToList();

                var purchasedByPackage = new Dictionary<Guid, int>();

                foreach (var packageId in servicePackageIds)
                {
                    var servicePackage = await _servicePackageRepository.GetByIdWithIncludesAsync(
                        packageId, cancellationToken, sp => sp.Items);

                    purchasedByPackage[packageId] = servicePackage?.GetTotalSessions() ?? 0;
                }

                // 3. Hareketler: yalnızca sayfadaki paketler için aktif kayıtlar.
                var customerPackageIds = pageItems.Select(p => p.Id).ToList();

                var usages = customerPackageIds.Count == 0
                    ? new List<PackageUsage>()
                    : (await _packageUsageRepository.FindAsync(
                        u => customerPackageIds.Contains(u.CustomerPackageId)
                            && u.Status == EnumPackageUsageStatus.Active
                            && !u.IsDeleted,
                        cancellationToken)).ToList();

                // 4. DTO: kalan hak = purchased - tüketilen - rezerve.
                var items = pageItems
                    .Select(p =>
                    {
                        var purchased = purchasedByPackage.GetValueOrDefault(p.ServicePackageId);
                        var consumed = usages.Count(u => u.CustomerPackageId == p.Id
                            && u.UsageType == EnumPackageUsageType.Consumption);
                        var reserved = usages.Count(u => u.CustomerPackageId == p.Id
                            && u.UsageType == EnumPackageUsageType.Reservation);

                        return new AdminCustomerPackageDto
                        {
                            Id = p.Id,
                            BusinessId = p.BusinessId,
                            CustomerId = p.CustomerId,
                            PackageId = p.ServicePackageId,
                            PurchasedAt = p.PurchasedAt,
                            ExpiresAt = p.ExpiresAt,
                            Status = p.Status,
                            Purchased = purchased,
                            Consumed = consumed,
                            Reserved = reserved,
                            Remaining = purchased - consumed - reserved
                        };
                    })
                    .ToList();

                return Result<AdminPagedResult<AdminCustomerPackageDto>>.Success(
                    new AdminPagedResult<AdminCustomerPackageDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<AdminCustomerPackageDto>>.Failure(
                    Error.Internal("Müşteri paketi listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
