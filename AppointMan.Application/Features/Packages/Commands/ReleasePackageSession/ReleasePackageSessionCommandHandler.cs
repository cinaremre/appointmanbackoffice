using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Packages.Commands.ReleasePackageSession
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
                    return Result<Guid>.Failure(Error.Conflict("Bu randevu icin aktif paket rezervasyonu bulunamadi."));

                // 2. Paket uzerinden yetki: isletme sahibi veya paketin musterisi.
                var customerPackage = await _customerPackageRepository.GetByIdAsync(
                    reservation.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Musteri paketi", reservation.CustomerPackageId.ToString()));

                if (!await CanActOnPackageAsync(customerPackage, cancellationToken))
                    return Result<Guid>.Failure(Error.Forbidden("Bu paket uzerinde islem yapma yetkiniz yok."));

                // 3. Rezervasyonu Release'e cevir; hareket artik kalan hak
                //    hesabina girmez.
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
                return Result<Guid>.Failure(Error.Internal("Paket hakki serbest birakilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>Caller paketin sahibi isletmenin mi, yoksa paket musterisinin kendisi mi?</summary>
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
