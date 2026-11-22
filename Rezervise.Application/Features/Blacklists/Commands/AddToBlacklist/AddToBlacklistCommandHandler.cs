using Rezervise.Application.Common.Errors;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommandHandler : IRequestHandler<AddToBlacklistCommand, Result<Guid>>
    {
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeService _dateTimeService;

        public AddToBlacklistCommandHandler(
            IRepository<BlacklistEntry> blacklistRepository,
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Employee> employeeRepository,
            IUnitOfWork unitOfWork,
            IDateTimeService dateTimeService)
        {
            _blacklistRepository = blacklistRepository;
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _appointmentRepository = appointmentRepository;
            _employeeRepository = employeeRepository;
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(AddToBlacklistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu - isletme sahibi veya isletmede gorevli calisan.
                var isOwner = business.OwnerId == request.RequesterId;

                if (!isOwner)
                {
                    var isEmployee = await _employeeRepository.ExistsAsync(
                        e => e.BusinessId == request.BusinessId &&
                             e.UserId == request.RequesterId &&
                             !e.IsDeleted,
                        cancellationToken);

                    if (!isEmployee)
                        return Result<Guid>.Failure(Error.Unauthorized(
                            "Bu islemi sadece isletme sahibi veya isletme calisani yapabilir."));
                }

                // 3. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 4. Musterinin bu isletmedeki acik randevulari: kara listeye ekleme
                //    aninda tamami iptal edilir (kullanici karari).
                var now = _dateTimeService.UtcNow;

                var openAppointments = await _appointmentRepository.FindAsync(
                    a => a.UserId == request.UserId &&
                         a.BusinessId == request.BusinessId &&
                         !a.IsDeleted &&
                         a.AppointmentDate > now &&
                         (a.Status == EnumAppointmentStatus.Pending ||
                          a.Status == EnumAppointmentStatus.Confirmed),
                    cancellationToken);

                // 5. On kontrol YAPILMAZ: (BusinessId, UserId) uzerindeki unique index
                //    tek dogruluk kaynagidir. Es zamanli iki ekleme denemesinde
                //    ikincisi DuplicateKeyException alir ve 409 Conflict doner.
                var entry = new BlacklistEntry(
                    request.UserId,
                    request.BusinessId,
                    openAppointments.Count,
                    request.Reason ?? "Manuel kara listeye ekleme");

                await _blacklistRepository.AddAsync(entry, cancellationToken);

                // 6. Acik randevular tek SaveChanges icinde iptal edilir; boylece
                //    kara liste kaydi ile iptaller ayni transaction'da kalir.
                foreach (var appointment in openAppointments)
                    appointment.Cancel("Kara listeye alindi", "Blacklist", now);

                // 7. Kaydet (unique index ihlali burada yakalanir)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 8. Kullanici 3 farkli isletmede kara listede mi?
                await CheckAndLockUserIfNeededAsync(request.UserId, cancellationToken);

                return Result<Guid>.Success(entry.Id);
            }
            catch (DuplicateKeyException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.DuplicateBlacklistEntry());
            }
            catch (ConcurrencyConflictException)
            {
                return Result<Guid>.Failure(ConcurrencyErrors.ConcurrencyConflict());
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kara listeye eklenirken bir hata olustu.", ex.Message));
            }
        }

        private async Task CheckAndLockUserIfNeededAsync(Guid userId, CancellationToken cancellationToken)
        {
            var entries = await _blacklistRepository.FindAsync(
                b => b.UserId == userId && !b.IsDeleted,
                cancellationToken);

            var distinctBusinessCount = entries
                .Select(b => b.BusinessId)
                .Distinct()
                .Count();

            if (distinctBusinessCount >= 3)
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user is not null && user.AccountStatus != EnumAccountStatus.Locked)
                {
                    user.LockAccount("3 farkli isletmede kara listeye girildi", "System");
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }
        }
    }
}
