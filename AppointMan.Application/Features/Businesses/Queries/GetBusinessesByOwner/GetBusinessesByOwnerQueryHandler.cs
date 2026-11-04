using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Helpers;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;
using System.Linq.Expressions;

namespace AppointMan.Application.Features.Businesses.Queries.GetBusinessesByOwner
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

                // DTO'ya donustur
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
                    Error.Internal("Isletmeler getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}