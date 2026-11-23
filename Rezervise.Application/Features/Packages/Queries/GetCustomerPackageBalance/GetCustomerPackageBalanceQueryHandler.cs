using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Queries.GetCustomerPackageBalance
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
                // 1. Müşteri paketi kontrolü
                var customerPackage = await _customerPackageRepository.GetByIdAsync(request.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.NotFound("Müşteri paketi", request.CustomerPackageId.ToString()));

                // 2. Yetki: paketin müşterisi veya işletme sahibi.
                var business = await _businessRepository.GetByIdAsync(customerPackage.BusinessId, cancellationToken);
                var isOwner = business is not null &&
                              CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);

                if (!isOwner && !CallerAuthorization.CanActAsUser(_currentUser, customerPackage.CustomerId))
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.Forbidden("Bu paketin bakiyesini görüntüleme yetkiniz yok."));

                // 3. Paket tanımının toplam seans sayısı.
                var servicePackage = await _servicePackageRepository.GetByIdWithIncludesAsync(
                    customerPackage.ServicePackageId, cancellationToken, p => p.Items);
                if (servicePackage is null)
                    return Result<CustomerPackageBalanceDto>.Failure(
                        Error.NotFound("Paket", customerPackage.ServicePackageId.ToString()));

                var purchasedTotal = servicePackage.GetTotalSessions();

                // 4. Hareketleri topla: kalan hak entity'de tutulmaz, burada hesaplanır.
                var activeUsages = await _packageUsageRepository.FindAsync(
                    u => u.CustomerPackageId == request.CustomerPackageId &&
                         u.Status == EnumPackageUsageStatus.Active &&
                         !u.IsDeleted,
                    cancellationToken);

                var activeReservations = activeUsages.Count(u => u.UsageType == EnumPackageUsageType.Reservation);
                var consumed = activeUsages.Count(u => u.UsageType == EnumPackageUsageType.Consumption);
                var remaining = purchasedTotal - activeReservations - consumed;

                // 5. DTO döndür.
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
                    Error.Internal("Paket bakiyesi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
