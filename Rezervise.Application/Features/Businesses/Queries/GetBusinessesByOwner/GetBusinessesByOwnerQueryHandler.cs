using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace Rezervise.Application.Features.Businesses.Queries.GetBusinessesByOwner
{
    public class GetBusinessesByOwnerQueryHandler : IRequestHandler<GetBusinessesByOwnerQuery, Result<List<BusinessDto>>>
    {
        private readonly IRepository<Business> _businessRepository;

        public GetBusinessesByOwnerQueryHandler(
            IRepository<Business> businessRepository)
        {
            _businessRepository = businessRepository;
        }

        public async Task<Result<List<BusinessDto>>> Handle(GetBusinessesByOwnerQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Filtreleme
                Expression<Func<Business, bool>> predicate = b => b.OwnerId == request.OwnerId && !b.IsDeleted;

                if (request.IsActive.HasValue)
                {
                    predicate = predicate.AndAlso(b => b.IsActive == request.IsActive.Value);
                }

                // Verileri getir
                var businesses = await _businessRepository.FindAsync(predicate, cancellationToken);

                // Sayfalama
                var pagedBusinesses = businesses
                    .OrderBy(b => b.Name)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                // DTO'ya dönüştür
                var dtos = pagedBusinesses.Select(b => new BusinessDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    BusinessType = b.BusinessType,
                    Address = b.Address?.GetFullAddress() ?? string.Empty,
                    PhoneNumber = b.PhoneNumber?.Value ?? string.Empty,
                    Email = b.Email?.Value ?? string.Empty,
                    WebsiteUrl = b.WebsiteUrl,
                    LogoUrl = b.LogoUrl,
                    IsActive = b.IsActive,
                    IsVerified = b.IsVerified,
                    Rating = b.Rating,
                    TotalReviews = b.TotalReviews,
                    OwnerId = b.OwnerId,
                    CreatedAt = b.CreatedAt
                }).ToList();

                return Result<List<BusinessDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<BusinessDto>>.Failure(
                    Error.Internal("İşletmeler getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}