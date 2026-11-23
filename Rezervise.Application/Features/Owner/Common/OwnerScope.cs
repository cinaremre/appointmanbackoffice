using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Application.Features.Owner.Common
{
    /// <summary>
    /// İşletme sahibi (BusinessOwner) uç'larının ortak sahiplik doğrulaması.
    /// Owner portalı tenant-scoped'dir: businessId istekten gelir ama yalnızca
    /// JWT'de çözülen kullanıcının SAHIP öldüğü işletmeler kabul edilir.
    /// Tüm Owner handler'lari işletme kapsamına girmeden önce bu kontrolü yapar.
    /// </summary>
    public static class OwnerScope
    {
        /// <summary>
        /// İşletmeyi sahiplik koşuluyla yükler. Kimlik doğrulanmamışsa
        /// Unauthorized, işletme başka bir şaibe aitse (veya yoksa) Forbidden
        /// döner. Başarı durumunda entity dondurulur.
        /// </summary>
        /// <param name="businessRepository">İşletme repository'si.</param>
        /// <param name="currentUser">JWT'den çözülen ambient kimlik.</param>
        /// <param name="businessId">İstekten gelen işletme kimliği.</param>
        /// <param name="cancellationToken">İptal token'i.</param>
        /// <returns>Sahip olunan işletme veya hata.</returns>
        public static async Task<Result<Business>> FindOwnedBusinessAsync(
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser,
            Guid businessId,
            CancellationToken cancellationToken)
        {
            // 1. Kimlik yoksa portal hiçbir şey açamaz.
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Result<Business>.Failure(Error.Unauthorized("Auth.Unauthorized"));

            var ownerId = currentUser.UserId.Value;

            // 2. Sahiplik tek sorguda doğrulanır: hem varlık hem sahibin
            //    aynı kullanıcı olması zorunludur. Boş sonucForbidden'dir;
            //    varlığın olmaması ile başkasının olması ayrım yapılmaz.
            var businesses = await businessRepository.FindAsync(
                b => b.Id == businessId && b.OwnerId == ownerId && !b.IsDeleted,
                cancellationToken);

            var business = businesses.FirstOrDefault();

            if (business is null)
                return Result<Business>.Failure(Error.Forbidden("Bu işletme size ait değil."));

            return Result<Business>.Success(business);
        }
    }
}
