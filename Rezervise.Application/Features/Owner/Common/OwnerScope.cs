using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Application.Features.Owner.Common
{
    /// <summary>
    /// Isletme sahibi (BusinessOwner) uc'larinin ortak sahiplik dogrulamasi.
    /// Owner portali tenant-scoped'dir: businessId istekten gelir ama yalnizca
    /// JWT'de cozulen kullanicinin SAHIP oldugu isletmeler kabul edilir.
    /// Tum Owner handler'lari isletme kapsamina girmeden once bu kontrolu yapar.
    /// </summary>
    public static class OwnerScope
    {
        /// <summary>
        /// Isletmeyi sahiplik kosuluyla yukler. Kimlik dogrulanmamissa
        /// Unauthorized, isletme baska bir saibe aitse (veya yoksa) Forbidden
        /// doner. Basari durumunda entity dondurulur.
        /// </summary>
        /// <param name="businessRepository">Isletme repository'si.</param>
        /// <param name="currentUser">JWT'den cozulen ambient kimlik.</param>
        /// <param name="businessId">Istekten gelen isletme kimligi.</param>
        /// <param name="cancellationToken">Iptal token'i.</param>
        /// <returns>Sahip olunan isletme veya hata.</returns>
        public static async Task<Result<Business>> FindOwnedBusinessAsync(
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser,
            Guid businessId,
            CancellationToken cancellationToken)
        {
            // 1. Kimlik yoksa portal hicbir sey acamaz.
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result<Business>.Failure(Error.Unauthorized("Auth.Unauthorized"));

            var ownerId = currentUser.UserId.Value;

            // 2. Sahiplik tek sorguda dogrulanir: hem varlik hem sahibin
            //    ayni kullanici olmasi zorunludur. Bos sonucForbidden'dir;
            //    varligin olmamasi ile baskasinin olmasi ayrim yapilmaz.
            var businesses = await businessRepository.FindAsync(
                b => b.Id == businessId && b.OwnerId == ownerId && !b.IsDeleted,
                cancellationToken);

            var business = businesses.FirstOrDefault();

            if (business is null)
                return Result<Business>.Failure(Error.Forbidden("Bu isletme size ait degil."));

            return Result<Business>.Success(business);
        }
    }
}
