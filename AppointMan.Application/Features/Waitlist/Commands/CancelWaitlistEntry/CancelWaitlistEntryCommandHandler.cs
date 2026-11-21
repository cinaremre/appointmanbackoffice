using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Waitlist.Commands.CancelWaitlistEntry
{
    public class CancelWaitlistEntryCommandHandler : IRequestHandler<CancelWaitlistEntryCommand, Result<Guid>>
    {
        private readonly IRepository<WaitlistEntry> _waitlistEntryRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public CancelWaitlistEntryCommandHandler(
            IRepository<WaitlistEntry> waitlistEntryRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _waitlistEntryRepository = waitlistEntryRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(CancelWaitlistEntryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kayit kontrolu
                var entry = await _waitlistEntryRepository.GetByIdAsync(request.EntryId, cancellationToken);
                if (entry is null)
                    return Result<Guid>.Failure(Error.NotFound("Bekleme listesi kaydi", request.EntryId.ToString()));

                // 2. Yetki: kaydin musterisi veya isletme sahibi.
                var isCustomer = CallerAuthorization.CanActAsUser(_currentUser, entry.CustomerId);

                var isOwner = false;
                if (!isCustomer)
                {
                    var business = await _businessRepository.GetByIdAsync(entry.BusinessId, cancellationToken);
                    isOwner = business is not null &&
                              CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);
                }

                if (!isCustomer && !isOwner)
                    return Result<Guid>.Failure(Error.Forbidden("Bu bekleme listesi kaydini iptal etme yetkiniz yok."));

                // 3. Kaydi iptal et
                entry.Cancel(_dateTime.UtcNow);

                await _waitlistEntryRepository.UpdateAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(entry.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Bekleme listesi kaydi iptal edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
