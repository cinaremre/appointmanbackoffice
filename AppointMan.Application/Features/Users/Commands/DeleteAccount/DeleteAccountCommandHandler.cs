using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;
using BCryptNet = BCrypt.Net.BCrypt;

namespace AppointMan.Application.Features.Users.Commands.DeleteAccount
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
        private readonly AppointManMetrics _metrics;

        /// <param name="userRepository">Kullanici deposu.</param>
        /// <param name="businessRepository">Isletme deposu; sahiplik kontrolu icin.</param>
        /// <param name="unitOfWork">Kalicilik birimi.</param>
        /// <param name="currentUser">Token'dan cozulen aktor kimligi.</param>
        /// <param name="metrics">KVKK hesap verebilirlik izi icin sayac.</param>
        public DeleteAccountCommandHandler(
            IRepository<User> userRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            AppointManMetrics metrics)
        {
            _userRepository = userRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _metrics = metrics;
        }

        /// <param name="request">Silme istegi; mevcut sifreyi tasir.</param>
        /// <param name="cancellationToken">Iptal belirteci.</param>
        /// <returns>Basarida <c>true</c>.</returns>
        public async Task<Result<bool>> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Aktor kimligi her zaman imzali token'dan gelir; istekteki
                //    hicbir alan kullaniciyi temsil etmez (IDOR korumasi).
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<bool>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                // 1. Kullaniciyi getir; silinmis kayit tekrar islenmez.
                var users = await _userRepository.FindAsync(
                    u => u.Id == currentUserId.Value && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();
                if (user is null)
                    return Result<bool>.Failure(Error.NotFound("User", currentUserId.Value.ToString()));

                // 2. Yikici islem icin sifre tekrar dogrulanir. Basarisiz deneme
                //    hesabi degistirmez; hata mesaji kimligi sizdirmaz.
                if (!BCryptNet.Verify(request.CurrentPassword, user.PasswordHash))
                    return Result<bool>.Failure(Error.Unauthorized("Auth.CurrentPasswordInvalid"));

                // 3. Isletme sahibi olan kullanici once devretmelidir (urun karari).
                //    Sahiplik ayri sorguyla kontrol edilir; navigation include edilmez
                //    cunku OwnedBusinesses'in EF'te eslenmis olmasi garanti degildir.
                var ownedBusinesses = await _businessRepository.FindAsync(
                    b => b.OwnerId == currentUserId.Value && !b.IsDeleted,
                    cancellationToken);

                if (ownedBusinesses.Any())
                    return Result<bool>.Failure(Error.BusinessRule("Auth.AccountDeletionBlockedByBusinesses"));

                // 4. Tek bir domain cagrisiyla tum kisisel veri silinir ve kayit
                //    soft delete edilir. Ranglar/yorumlar/favoriler/bildirimler
                //    SILINMEZ; anonim kullaniciya bagli kalir.
                user.AnonymizeAndDelete(DateTime.UtcNow);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // KVKK hesap verebilirlik izi: kimlik tasimayan sayac. Kisisel veri
                // (e-posta, telefon, sebep) bilincli olarak kaydedilmez.
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
                    Error.Internal("Hesap silinirken bir hata olustu.", ex.Message));
            }
        }
    }
}
