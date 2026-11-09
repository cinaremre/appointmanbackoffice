using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommandHandler : IRequestHandler<AddToBlacklistCommand, Result<Guid>>
    {
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddToBlacklistCommandHandler(
            IRepository<BlacklistEntry> blacklistRepository,
            IRepository<Business> businessRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork)
        {
            _blacklistRepository = blacklistRepository;
            _businessRepository = businessRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(AddToBlacklistCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu - sadece isletme sahibi
                if (business.OwnerId != request.RequesterId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Bu islemi sadece isletme sahibi yapabilir."));

                // 3. Kullanici kontrolu
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                if (user is null)
                    return Result<Guid>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

                // 4. Zaten kara listede mi?
                var existingEntries = await _blacklistRepository.FindAsync(
                    b => b.UserId == request.UserId &&
                         b.BusinessId == request.BusinessId &&
                         !b.IsDeleted,
                    cancellationToken);

                if (existingEntries.Any())
                    return Result<Guid>.Failure(
                        Error.Conflict("Bu kullanici zaten kara listede."));

                // 5. Yeni kara liste kaydi olustur
                var entry = new BlacklistEntry(
                    request.UserId,
                    request.BusinessId,
                    0,  // Manuel ekleme, randevu sayisi 0
                    request.Reason ?? "Manuel kara listeye ekleme");

                // 6. Kaydet
                await _blacklistRepository.AddAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 7. Kullanici 3 farkli isletmede kara listede mi?
                await CheckAndLockUserIfNeededAsync(request.UserId, cancellationToken);

                return Result<Guid>.Success(entry.Id);
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