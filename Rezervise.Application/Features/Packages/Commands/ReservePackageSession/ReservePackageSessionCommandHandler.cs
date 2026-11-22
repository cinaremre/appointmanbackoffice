using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ReservePackageSession
{
    public class ReservePackageSessionCommandHandler : IRequestHandler<ReservePackageSessionCommand, Result<Guid>>
    {
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<ServicePackage> _servicePackageRepository;
        private readonly IRepository<PackageUsage> _packageUsageRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public ReservePackageSessionCommandHandler(
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<ServicePackage> servicePackageRepository,
            IRepository<PackageUsage> packageUsageRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _customerPackageRepository = customerPackageRepository;
            _servicePackageRepository = servicePackageRepository;
            _packageUsageRepository = packageUsageRepository;
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(ReservePackageSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Musteri paketi kontrolu
                var customerPackage = await _customerPackageRepository.GetByIdAsync(request.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Musteri paketi", request.CustomerPackageId.ToString()));

                // 2. Yetki: isletme sahibi veya paketin musterisi.
                if (!await CanActOnPackageAsync(customerPackage, cancellationToken))
                    return Result<Guid>.Failure(Error.Forbidden("Bu paket uzerinde islem yapma yetkiniz yok."));

                // 3. Randevu kontrolu: ayni isletme, ayni hizmet, paketin musterisi ve aktif olmali.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                if (appointment.BusinessId != customerPackage.BusinessId ||
                    appointment.ServiceId != request.ServiceId ||
                    appointment.UserId != customerPackage.CustomerId)
                    return Result<Guid>.Failure(Error.Conflict("Randevu, paketin isletme/hizmet/musteri bilgileriyle uyumsuz."));

                if (appointment.Status is not (EnumAppointmentStatus.Pending or EnumAppointmentStatus.Confirmed))
                    return Result<Guid>.Failure(Error.Conflict("Paket hakki yalnizca aktif randevular icin ayrilabilir."));

                // 4. Hizmet pakete dahil olmali
                var servicePackage = await _servicePackageRepository.GetByIdWithIncludesAsync(
                    customerPackage.ServicePackageId, cancellationToken, p => p.Items);
                if (servicePackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Paket", customerPackage.ServicePackageId.ToString()));

                var item = servicePackage.Items
                    .FirstOrDefault(i => i.ServiceId == request.ServiceId && !i.IsDeleted);

                if (item is null)
                    return Result<Guid>.Failure(Error.Validation("Bu hizmet paketin kapsaminda degil.", $"ServiceId: {request.ServiceId}"));

                // 5. Paket durumu ve gecerlilik kontrolu
                var now = _dateTime.UtcNow;
                if (customerPackage.Status != EnumCustomerPackageStatus.Active)
                    return Result<Guid>.Failure(Error.Conflict("Musteri paketi aktif degil."));

                if (customerPackage.ExpiresAt <= now)
                    return Result<Guid>.Failure(Error.Conflict("Paketin gecerlilik suresi dolmus."));

                // 6. Ayni randevu icin ikinci rezervasyon/tuketim engellenir:
                //    ayni hak iki randevuda kullanilamaz ve cift islem onlenir.
                var existingForAppointment = await _packageUsageRepository.FindAsync(
                    u => u.CustomerPackageId == request.CustomerPackageId &&
                         u.AppointmentId == request.AppointmentId &&
                         u.Status == EnumPackageUsageStatus.Active,
                    cancellationToken);

                if (existingForAppointment.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu randevu icin zaten paket hakki kullanilmis."));

                // 7. Kalan hak kontrolu: kalan = purchased toplam - aktif rezervasyon - tuketim.
                var activeUsages = await _packageUsageRepository.FindAsync(
                    u => u.CustomerPackageId == request.CustomerPackageId &&
                         u.Status == EnumPackageUsageStatus.Active &&
                         (u.UsageType == EnumPackageUsageType.Reservation ||
                          u.UsageType == EnumPackageUsageType.Consumption),
                    cancellationToken);

                var purchasedTotal = servicePackage.GetTotalSessions();
                var remaining = purchasedTotal - activeUsages.Count;

                if (remaining <= 0)
                    return Result<Guid>.Failure(Error.Conflict("Paketin kalan hakki yetersiz.", $"Kalan: {remaining}"));

                // 8. Rezervasyon hareketini olustur
                var usage = PackageUsage.Reserve(
                    request.CustomerPackageId,
                    request.ServiceId,
                    request.AppointmentId,
                    now);

                await _packageUsageRepository.AddAsync(usage, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(usage.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Paket hakki ayrilirken bir hata olustu.", ex.Message));
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
