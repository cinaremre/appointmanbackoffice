using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ReleasePackageSession
{
    public class ReleasePackageSessionCommandHandler : IRequestHandler<ReleasePackageSessionCommand, Result<Guid>>
    {
        private readonly IRepository<PackageUsage> _packageUsageRepository;
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public ReleasePackageSessionCommandHandler(
            IRepository<PackageUsage> packageUsageRepository,
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _packageUsageRepository = packageUsageRepository;
            _customerPackageRepository = customerPackageRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(ReleasePackageSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Randevunun aktif Reservation hareketini bul.
                var reservations = await _packageUsageRepository.FindAsync(
                    u => u.AppointmentId == request.AppointmentId &&
                         u.UsageType == EnumPackageUsageType.Reservation &&
                         u.Status == EnumPackageUsageStatus.Active,
                    cancellationToken);

                var reservation = reservations.FirstOrDefault();
                if (reservation is null)
                    return Result<Guid>.Failure(Error.Conflict("Bu randevu için aktif paket rezervasyonu bulunamadı."));

                // 2. Paket üzerinden yetki: işletme sahibi veya paketin müşterisi.
                var customerPackage = await _customerPackageRepository.GetByIdAsync(
                    reservation.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Müşteri paketi", reservation.CustomerPackageId.ToString()));

                if (!await CanActOnPackageAsync(customerPackage, cancellationToken))
                    return Result<Guid>.Failure(Error.Forbidden("Bu paket üzerinde işlem yapma yetkiniz yok."));

                // 3. Rezervasyonu Release'e çevir; hareket artık kalan hak
                //    hesabına girmez.
                var now = _dateTime.UtcNow;
                reservation.Release(now);

                await _packageUsageRepository.UpdateAsync(reservation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(reservation.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Paket hakkı serbest bırakılırken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>Caller paketin sahibi işletmenin mi, yoksa paket müşterisinin kendisi mi?</summary>
        private async Task<bool> CanActOnPackageAsync(CustomerPackage customerPackage, CancellationToken cancellationToken)
        {
            if (CallerAuthorization.CanActAsUser(_currentUser, customerPackage.CustomerId))
                return true;

            var business = await _businessRepository.GetByIdAsync(customerPackage.BusinessId, cancellationToken);
            return business is not null &&
                   CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);
        }
    }
}
