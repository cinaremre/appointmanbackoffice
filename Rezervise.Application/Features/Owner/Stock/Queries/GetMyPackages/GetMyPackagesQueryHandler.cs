using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Stock.Queries.GetMyPackages
{
    /// <summary>
    /// İşletme sahibinin müşteri paketi listesi. Sahiplik OwnerScope ile
    /// doğrulanır; kalan hak hareketlerden hesaplanır
    /// (GetAdminCustomerPackages deseniyle aynı).
    /// </summary>
    public class GetMyPackagesQueryHandler
        : IRequestHandler<GetMyPackagesQuery, Result<AdminPagedResult<OwnerCustomerPackageDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<PackageUsage> _packageUsageRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyPackagesQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<PackageUsage> packageUsageRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _customerPackageRepository = customerPackageRepository;
            _servicePackageRepository = servicePackageRepository;
            _packageUsageRepository = packageUsageRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerCustomerPackageDto>>> Handle(
            GetMyPackagesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerCustomerPackageDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var packages = await _customerPackageRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId && !p.IsDeleted,
                    cancellationToken);

                var ordered = packages.OrderByDescending(p => p.PurchasedAt).ToList();

                var pageItems = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // 3. Purchased: paket tanımındaki toplam seans sayısı
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

                // 4. Hareketler: yalnızca sayfadaki paketler için aktif kayıtlar.
                var customerPackageIds = pageItems.Select(p => p.Id).ToList();

                var usages = customerPackageIds.Count == 0
                    ? new List<PackageUsage>()
                    : (await _packageUsageRepository.FindAsync(
                        u => customerPackageIds.Contains(u.CustomerPackageId)
                            && u.Status == EnumPackageUsageStatus.Active
                            && !u.IsDeleted,
                        cancellationToken)).ToList();

                // 5. DTO: kalan hak = purchased - tüketilen - rezerve.
                var items = pageItems
                    .Select(p =>
                    {
                        var purchased = purchasedByPackage.GetValueOrDefault(p.ServicePackageId);
                        var consumed = usages.Count(u => u.CustomerPackageId == p.Id
                            && u.UsageType == EnumPackageUsageType.Consumption);
                        var reserved = usages.Count(u => u.CustomerPackageId == p.Id
                            && u.UsageType == EnumPackageUsageType.Reservation);

                        return new OwnerCustomerPackageDto
                        {
                            Id = p.Id,
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

                return Result<AdminPagedResult<OwnerCustomerPackageDto>>.Success(
                    new AdminPagedResult<OwnerCustomerPackageDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerCustomerPackageDto>>.Failure(
                    Error.Internal("Müşteri paketi listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
