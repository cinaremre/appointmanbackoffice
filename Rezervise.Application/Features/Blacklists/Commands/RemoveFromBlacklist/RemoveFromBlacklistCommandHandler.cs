using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Commands.RemoveFromBlacklist
{
    public class RemoveFromBlacklistCommandHandler
        : IRequestHandler<RemoveFromBlacklistCommand, Result<Guid>>
    {
        private readonly IRepository<BlacklistEntry> _blacklistRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveFromBlacklistCommandHandler(
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

        public async Task<Result<Guid>> Handle(
            RemoveFromBlacklistCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü
                if (business.OwnerId != request.RequesterId)
                    return Result<Guid>.Failure(
                        Error.Unauthorized("Bu işlemi sadece işletme sahibi yapabilir."));

                // 3. Kara liste kaydı kontrolü
                var entry = await _blacklistRepository.GetByIdAsync(request.BlacklistEntryId, cancellationToken);
                if (entry is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Kara liste kaydı", request.BlacklistEntryId.ToString()));

                // 4. Kayıt bu işletmeye mi ait?
                if (entry.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(
                        Error.BusinessRule("Bu kara liste kaydı işletmeye ait değil."));

                // 5. Zaten çıkarılmış mi?
                if (entry.IsDeleted)
                    return Result<Guid>.Failure(
                        Error.Conflict("Bu kayıt zaten kara listeden çıkarılmış."));

                // 6. Soft delete + bilgileri kaydet
                entry.Remove(request.RequesterId.ToString(), request.Reason);

                // 7. Kullanıcı Locked ise Active'e çevir (opsiyonel)
                var user = await _userRepository.GetByIdAsync(entry.UserId, cancellationToken);
                if (user is not null && user.AccountStatus == Domain.Enums.EnumAccountStatus.Locked)
                {
                    // Kullanıcının hala başka işletmelerde kara listede olup olmadığını kontrol et
                    var remainingEntries = await _blacklistRepository.FindAsync(
                        b => b.UserId == user.Id && !b.IsDeleted,
                        cancellationToken);

                    if (!remainingEntries.Any())
                    {
                        user.UnlockAccount(request.RequesterId.ToString());
                    }
                }

                // 8. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(entry.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kara listeden çıkarılırken bir hata oluştu.", ex.Message));
            }
        }
    }
}