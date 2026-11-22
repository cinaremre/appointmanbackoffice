using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.PublicPages.Commands.PublishPublicPage
{
    public class PublishPublicPageCommandHandler : IRequestHandler<PublishPublicPageCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessPublicPage> _pageRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public PublishPublicPageCommandHandler(
            IRepository<BusinessPublicPage> pageRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _pageRepository = pageRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(PublishPublicPageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmenin public sayfasini yayina alma yetkiniz yok."));

                // 3. Isletmeye ait mevcut sayfa: varsa guncelle, yoksa olustur.
                var existingPage = (await _pageRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId && !p.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                BusinessPublicPage page;
                if (existingPage is null)
                {
                    page = new BusinessPublicPage(
                        request.BusinessId,
                        request.Slug,
                        request.ThemeSettings,
                        request.IsPublished);
                }
                else
                {
                    page = existingPage;
                    page.SetSlug(request.Slug);
                    page.SetThemeSettings(request.ThemeSettings);

                    if (request.IsPublished)
                        page.Publish();
                    else
                        page.Unpublish();
                }

                // 4. Slug baska isletmede kullaniliyor mu?
                //    (Domain kural ihlali DomainException ile, sahiplik cakismasi Conflict ile doner.)
                var slugTakenByOther = (await _pageRepository.FindAsync(
                    p => p.Slug == page.Slug && p.BusinessId != request.BusinessId && !p.IsDeleted,
                    cancellationToken)).Any();

                if (slugTakenByOther)
                    return Result<Guid>.Failure(Error.Conflict("Bu slug baska bir isletme tarafindan kullaniliyor."));

                // 5. Kaydet
                if (existingPage is null)
                    await _pageRepository.AddAsync(page, cancellationToken);
                else
                    await _pageRepository.UpdateAsync(page, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(page.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Public sayfa yayinlanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
