using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Packages.Commands.ConsumePackageSession
{
    public class ConsumePackageSessionCommandHandler : IRequestHandler<ConsumePackageSessionCommand, Result<Guid>>
    {
        private readonly IRepository<CustomerPackage> _customerPackageRepository;
        private readonly IRepository<PackageUsage> _packageUsageRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public ConsumePackageSessionCommandHandler(
            IRepository<CustomerPackage> customerPackageRepository,
            IRepository<PackageUsage> packageUsageRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _customerPackageRepository = customerPackageRepository;
            _packageUsageRepository = packageUsageRepository;
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(ConsumePackageSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Müşteri paketi kontrolü
                var customerPackage = await _customerPackageRepository.GetByIdAsync(request.CustomerPackageId, cancellationToken);
                if (customerPackage is null)
                    return Result<Guid>.Failure(Error.NotFound("Müşteri paketi", request.CustomerPackageId.ToString()));

                // 2. Yetki: işletme sahibi veya paketin müşterisi.
                if (!await CanActOnPackageAsync(customerPackage, cancellationToken))
                    return Result<Guid>.Failure(Error.Forbidden("Bu paket üzerinde işlem yapma yetkiniz yok."));

                // 3. Randevu kontrolü: tüketim için randevu Completed olmalı.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null)
                    return Result<Guid>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                if (appointment.Status != EnumAppointmentStatus.Completed)
                    return Result<Guid>.Failure(Error.Conflict("Paket hakkı yalnızca tamamlanan randevular için tüketilebilir."));

                if (appointment.BusinessId != customerPackage.BusinessId ||
                    appointment.ServiceId != request.ServiceId ||
                    appointment.UserId != customerPackage.CustomerId)
                    return Result<Guid>.Failure(Error.Conflict("Randevu, paketin işletme/hizmet/müşteri bilgileriyle uyumsuz."));

                // 4. Randevunun aktif Reservation hareketi olmalı.
                var reservations = await _packageUsageRepository.FindAsync(
                    u => u.CustomerPackageId == request.CustomerPackageId &&
                         u.AppointmentId == request.AppointmentId &&
                         u.UsageType == EnumPackageUsageType.Reservation &&
                         u.Status == EnumPackageUsageStatus.Active,
                    cancellationToken);

                var reservation = reservations.FirstOrDefault();
                if (reservation is null)
                    return Result<Guid>.Failure(Error.Conflict("Bu randevu için aktif paket rezervasyonu bulunamadı."));

                // 5. Rezervasyon hareketini iptal et ve Consumption hareketi ekle.
                //    İki değişiklik aynı SaveChanges ile tek transaction'da kalır.
                var now = _dateTime.UtcNow;
                reservation.Cancel(now);

                var consumption = PackageUsage.Consume(
                    request.CustomerPackageId,
                    request.ServiceId,
                    request.AppointmentId,
                    now);

                await _packageUsageRepository.AddAsync(consumption, cancellationToken);
                await _packageUsageRepository.UpdateAsync(reservation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(consumption.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Paket hakkı tüketilirken bir hata oluştu.", ex.Message));
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
