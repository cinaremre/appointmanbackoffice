using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.PublicPages.Queries.GetPublicPageBySlug
{
    /// <summary>
    /// Anonim public sayfa sorgusu. Yetki kontrolü YOKTUR; handler
    /// ICurrentUserService'e hiç bağlanmaz. Slug normalize edilerek
    /// eşlenir; yayındaki sayfa ve aktif hizmetler döner.
    /// </summary>
    public class GetPublicPageBySlugQueryHandler : IRequestHandler<GetPublicPageBySlugQuery, Result<PublicPageDto>>
    {
        private readonly IRepository<BusinessPublicPage> _pageRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Service> _serviceRepository;

        public GetPublicPageBySlugQueryHandler(
            IRepository<BusinessPublicPage> pageRepository,
            IRepository<Business> businessRepository,
            IRepository<Service> serviceRepository)
        {
            _pageRepository = pageRepository;
            _businessRepository = businessRepository;
            _serviceRepository = serviceRepository;
        }

        public async Task<Result<PublicPageDto>> Handle(GetPublicPageBySlugQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Slug normalize (entity ile aynı kural: küçük harf).
                var slug = (request.Slug ?? string.Empty).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(slug))
                    return Result<PublicPageDto>.Failure(Error.Validation("Slug boş olamaz."));

                // 2. Yayındaki sayfa. Yayında değilse NotFound döner.
                var page = (await _pageRepository.FindAsync(
                    p => p.Slug == slug && p.IsPublished && !p.IsDeleted,
                    cancellationToken)).FirstOrDefault();

                if (page is null)
                    return Result<PublicPageDto>.Failure(Error.NotFound("Public sayfa", slug));

                // 3. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(page.BusinessId, cancellationToken);
                if (business is null || !business.IsActive)
                    return Result<PublicPageDto>.Failure(Error.NotFound("İşletme", page.BusinessId.ToString()));

                // 4. Aktif hizmetler
                var services = await _serviceRepository.FindAsync(
                    s => s.BusinessId == page.BusinessId && s.IsActive && !s.IsDeleted,
                    cancellationToken);

                // 5. DTO oluştur (kişisel veri yok)
                var dto = new PublicPageDto
                {
                    BusinessId = business.Id,
                    BusinessName = business.Name,
                    Slug = page.Slug,
                    Services = services.Select(s => new PublicPageServiceDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Price = s.Price?.Amount,
                        DurationMinutes = s.DurationInMinutes
                    }).ToList()
                };

                return Result<PublicPageDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<PublicPageDto>.Failure(
                    Error.Internal("Public sayfa getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
