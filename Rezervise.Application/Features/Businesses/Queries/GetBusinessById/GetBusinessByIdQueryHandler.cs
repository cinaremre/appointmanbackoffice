using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Businesses.Queries.GetBusinessById
{
    public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, Result<BusinessDto>>
    {
        private readonly IRepository<Business> _businessRepository;

        public GetBusinessByIdQueryHandler(IRepository<Business> businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<Result<BusinessDto>> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletmeyi ID'ye göre getir
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                // 2. İşletme bulunamadıysa hata döndür
                if (business is null)
                    return Result<BusinessDto>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Verileri DTO'ya dönüştür
                var dto = new BusinessDto
                {
                    Id = business.Id,
                    Name = business.Name,
                    Description = business.Description,
                    BusinessType = business.BusinessType,
                    Address = business.Address?.GetFullAddress() ?? string.Empty,
                    PhoneNumber = business.PhoneNumber?.Value ?? string.Empty,
                    Email = business.Email?.Value ?? string.Empty,
                    WebsiteUrl = business.WebsiteUrl,
                    LogoUrl = business.LogoUrl,
                    IsActive = business.IsActive,
                    IsVerified = business.IsVerified,
                    Rating = business.Rating,
                    TotalReviews = business.TotalReviews,
                    OwnerId = business.OwnerId,
                    CreatedAt = business.CreatedAt
                };

                return Result<BusinessDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BusinessDto>.Failure(Error.Internal("İşletme bilgileri getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}