using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.CancelWaitlistEntry
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
                // 1. Kayıt kontrolü
                var entry = await _waitlistEntryRepository.GetByIdAsync(request.EntryId, cancellationToken);
                if (entry is null)
                    return Result<Guid>.Failure(Error.NotFound("Bekleme listesi kaydı", request.EntryId.ToString()));

                // 2. Yetki: kaydın müşterisi veya işletme sahibi.
                var isCustomer = CallerAuthorization.CanActAsUser(_currentUser, entry.CustomerId);

                var isOwner = false;
                if (!isCustomer)
                {
                    var business = await _businessRepository.GetByIdAsync(entry.BusinessId, cancellationToken);
                    isOwner = business is not null &&
                              CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId);
                }

                if (!isCustomer && !isOwner)
                    return Result<Guid>.Failure(Error.Forbidden("Bu bekleme listesi kaydını iptal etme yetkiniz yok."));

                // 3. Kaydı iptal et
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
                return Result<Guid>.Failure(Error.Internal("Bekleme listesi kaydı iptal edilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
