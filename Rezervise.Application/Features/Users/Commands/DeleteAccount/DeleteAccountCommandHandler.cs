using Rezervise.Application.Common.Diagnostics;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Rezervise.Application.Features.Users.Commands.DeleteAccount
{
    /// <summary>
    /// Anonymizes and soft deletes the authenticated user's account.
    /// The account row is kept because appointment, review, favourite, notification
    /// and blacklist rows reference it through Restrict foreign keys.
    /// </summary>
    public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result<bool>>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly RezerviseMetrics _metrics;

        /// <param name="userRepository">Kullanıcı deposu.</param>
        /// <param name="businessRepository">İşletme deposu; sahiplik kontrolü için.</param>
        /// <param name="unitOfWork">Kalıcılık birimi.</param>
        /// <param name="currentUser">Token'dan çözülen aktör kimliği.</param>
        /// <param name="metrics">KVKK hesap verebilirlik izi için sayaç.</param>
        public DeleteAccountCommandHandler(
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            RezerviseMetrics metrics)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _metrics = metrics;
        }

        /// <param name="request">Silme isteği; mevcut şifreyi taşır.</param>
        /// <param name="cancellationToken">İptal belirteci.</param>
        /// <returns>Başarıda <c>true</c>.</returns>
        public async Task<Result<bool>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Aktör kimliği her zaman imzalı token'dan gelir; istekteki
                //    hiçbir alan kullanıcıyı temsil etmez (IDOR koruması).
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<bool>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                // 1. Kullanıcıyı getir; silinmiş kayıt tekrar işlenmez.
                var users = await _userRepository.FindAsync(
                    u => u.Id == currentUserId.Value && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<bool>.Failure(Error.NotFound("User", currentUserId.Value.ToString()));

                // 2. Yıkıcı işlem için şifre tekrar doğrulanır. Başarısız deneme
                //    hesabı değiştirmez; hata mesajı kimliği sızdırmaz.
                if (!BCryptNet.Verify(request.CurrentPassword, user.PasswordHash))
                    return Result<bool>.Failure(Error.Unauthorized("Auth.CurrentPasswordInvalid"));

                // 3. İşletme sahibi olan kullanıcı önce devretmelidir (ürün kararı).
                //    Sahiplik ayrı sorguyla kontrol edilir; navigation include edilmez
                //    çünkü OwnedBusinesses'in EF'te eşlenmiş olması garanti değildir.
                var ownedBusinesses = await _businessRepository.FindAsync(
                    b => b.OwnerId == currentUserId.Value && !b.IsDeleted,
                    cancellationToken);

                if (ownedBusinesses.Any())
                    return Result<bool>.Failure(Error.BusinessRule("Auth.AccountDeletionBlockedByBusinesses"));

                // 4. Tek bir domain çağrısıyla tüm kişisel veri silinir ve kayıt
                //    soft delete edilir. Ranglar/yorumlar/favoriler/bildirimler
                //    SILINMEZ; anonim kullanıcıya bağlı kalır.
                user.AnonymizeAndDelete(DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // KVKK hesap verebilirlik izi: kimlik taşımayan sayaç. Kişisel veri
                // (e-posta, telefon, sebep) bilinçli olarak kaydedilmez.
                _metrics.RecordAccountDeleted();

                return Result<bool>.Success(true);
            }
            catch (DomainException ex)
            {
                return Result<bool>.Failure(Error.BusinessRule(ex.Key));
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(
                    Error.Internal("Hesap silinirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
