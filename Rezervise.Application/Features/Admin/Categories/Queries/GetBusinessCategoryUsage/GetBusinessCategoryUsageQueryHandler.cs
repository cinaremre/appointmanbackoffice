using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryUsage
{
    public class GetBusinessCategoryUsageQueryHandler
        : IRequestHandler<GetBusinessCategoryUsageQuery, Result<BusinessCategoryUsageDto>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IRepository<Business> _businessRepository;

        public GetBusinessCategoryUsageQueryHandler(
            IRepository<BusinessCategory> categoryRepository,
            IRepository<Business> businessRepository)
        {
            _categoryRepository = categoryRepository;
            _businessRepository = businessRepository;
        }

        public async Task<Result<BusinessCategoryUsageDto>> Handle(
            GetBusinessCategoryUsageQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(
                    request.CategoryId, cancellationToken);

                if (category is null)
                    return Result<BusinessCategoryUsageDto>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // Business.CategoryId üzerinden sayım; silinmiş işletmeler hariç.
                var businesses = await _businessRepository.FindAsync(
                    b => b.CategoryId == request.CategoryId && !b.IsDeleted,
                    cancellationToken);

                var dto = new BusinessCategoryUsageDto
                {
                    CategoryId = category.Id,
                    Name = category.Name,
                    BusinessCount = businesses.Count
                };

                return Result<BusinessCategoryUsageDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<BusinessCategoryUsageDto>.Failure(
                    Error.Internal("Kategori kullanım bilgisi getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
