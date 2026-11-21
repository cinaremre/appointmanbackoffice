using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Packages.Queries.GetCustomerPackageBalance
{
    public class GetCustomerPackageBalanceQueryHandler : IRequestHandler<GetCustomerPackageBalanceQuery, Result<CustomerPackageBalanceDto>>
    {
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<PackageUsage> _packageUsageRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetCustomerPackageBalanceQueryHandler(
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<PackageUsage> packageUsageRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _customerPackageRepository = customerPackageRepository;
            _servicePackageRepository = servicePackageRepository;
            _packageUsageRepository = packageUsageRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<CustomerPackageBalanceDto>> Handle(GetCustomerPackageBalanceQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Musteri paketi kontrolu
                var customerPackage = await _customerPackageRepository.GetByIdAsync(request.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.NotFound("Musteri paketi", request.CustomerPackageId.ToString()));

                // 2. Yetki: paketin musterisi veya isletme sahibi.
                var business = await _businessRepository.GetByIdAsync(customerPackage.BusinessId, cancellationToken);
                var isOwner = business is not null &&
                              CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);

                if (!isOwner && !CallerAuthorization.CanActAsUser(_currentUser, customerPackage.CustomerId))
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.Forbidden("Bu paketin bakiyesini goruntuleme yetkiniz yok."));

                // 3. Paket taniminin toplam seans sayisi.
                var servicePackage = await _servicePackageRepository.GetByIdWithIncludesAsync(
                    customerPackage.ServicePackageId, cancellationToken, p => p.Items);
                if (servicePackage is null)
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.NotFound("Paket", customerPackage.ServicePackageId.ToString()));

                var purchasedTotal = servicePackage.GetTotalSessions();

                // 4. Hareketleri topla: kalan hak entity'de tutulmaz, burada hesaplanir.
                var activeUsages = await _packageUsageRepository.FindAsync(
                    u => u.CustomerPackageId == request.CustomerPackageId &&
                         u.Status == EnumPackageUsageStatus.Active &&
                         !u.IsDeleted,
                    cancellationToken);

                var activeReservations = activeUsages.Count(u => u.UsageType == EnumPackageUsageType.Reservation);
                var consumed = activeUsages.Count(u => u.UsageType == EnumPackageUsageType.Consumption);
                var remaining = purchasedTotal - activeReservations - consumed;

                // 5. DTO dondur.
                return Result<CustomerPackageBalanceDto>.Success(new CustomerPackageBalanceDto
                {
                    CustomerPackageId = customerPackage.Id,
                    ServicePackageId = customerPackage.ServicePackageId,
                    BusinessId = customerPackage.BusinessId,
                    CustomerId = customerPackage.CustomerId,
                    Status = customerPackage.Status,
                    ExpiresAt = customerPackage.ExpiresAt,
                    PurchasedTotal = purchasedTotal,
                    ActiveReservations = activeReservations,
                    Consumed = consumed,
                    Remaining = remaining
                });
            }
            catch (DomainException ex)
            {
                return Result<CustomerPackageBalanceDto>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<CustomerPackageBalanceDto>.Failure(
                    Error.Internal("Paket bakiyesi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
