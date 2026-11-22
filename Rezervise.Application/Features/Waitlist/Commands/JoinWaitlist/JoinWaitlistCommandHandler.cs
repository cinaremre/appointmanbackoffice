using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Waitlist.Commands.JoinWaitlist
{
    public class JoinWaitlistCommandHandler : IRequestHandler<JoinWaitlistCommand, Result<Guid>>
    {
        private readonly IRepository<WaitlistEntry> _waitlistEntryRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public JoinWaitlistCommandHandler(
            IRepository<WaitlistEntry> waitlistEntryRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _waitlistEntryRepository = waitlistEntryRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(JoinWaitlistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Hizmet kontrolu: ayni isletmenin hizmeti olmali.
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Validation("Hizmet bu isletmeye ait degil.", $"ServiceId: {request.ServiceId}"));

                // 3. Hedef musteriyi belirle:
                //    - Isletme sahibi baskasi adina kayit acabilir (CustomerId govdeden).
                //    - Musteri kendi adina kayit acar; kimlik JWT'den cozulur,
                //      govdede baska bir musteri ID verilmisse kabul edilmez.
                Guid customerId;
                if (CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                {
                    if (request.CustomerId == Guid.Empty)
                        return Result<Guid>.Failure(Error.Validation("Isletme adina kayit acilirken musteri ID belirtilmelidir."));

                    customerId = request.CustomerId;
                }
                else if (_currentUser.IsAuthenticated &&
                         _currentUser.UserId.HasValue &&
                         (request.CustomerId == Guid.Empty || request.CustomerId == _currentUser.UserId.Value))
                {
                    customerId = _currentUser.UserId.Value;
                }
                else
                {
                    return Result<Guid>.Failure(Error.Forbidden("Bekleme listesine yalnizca kendi adiniza veya isletme sahibi olarak kayit acabilirsiniz."));
                }

                // 4. Musteri kontrolu
                var customer = await _userRepository.GetByIdAsync(customerId, cancellationToken);
                if (customer is null)
                    return Result<Guid>.Failure(Error.NotFound("Musteri", customerId.ToString()));

                // 5. Ayni musteri + ayni hizmet icin aktif kayit engellenir.
                var existing = await _waitlistEntryRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId &&
                         e.CustomerId == customerId &&
                         e.ServiceId == request.ServiceId &&
                         e.Status == EnumWaitlistEntryStatus.Active &&
                         !e.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu musteri icin ayni hizmette zaten aktif bir bekleme listesi kaydi var."));

                // 6. Kaydi olustur
                var entry = new WaitlistEntry(
                    request.BusinessId,
                    customerId,
                    request.ServiceId,
                    request.EarliestDate,
                    request.LatestDate,
                    request.PreferredEmployeeId,
                    request.PreferredTimeRange);

                await _waitlistEntryRepository.AddAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(entry.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Bekleme listesine kayit eklenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
