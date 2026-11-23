using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Commands.CreateCategory
{
    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCategoryCommandHandler(
            IRepository<BusinessCategory> categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Aynı isimde kategori var mi?
                var existingCategories = await _categoryRepository.FindAsync(
                    c => c.Name == request.Name && !c.IsDeleted,
                    cancellationToken);

                if (existingCategories.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir kategori zaten mevcut."));

                // 2. Parent kategori kontrolü
                BusinessCategory? parentCategory = null;
                if (request.ParentCategoryId.HasValue)
                {
                    parentCategory = await _categoryRepository.GetByIdAsync(
                        request.ParentCategoryId.Value, cancellationToken);

                    if (parentCategory is null)
                        return Result<Guid>.Failure(Error.NotFound("Üst kategori", request.ParentCategoryId.Value.ToString()));
                }

                // 3. Kategori oluştur
                var category = new BusinessCategory(
                    request.Name,
                    request.DisplayOrder,
                    request.CorrespondingBusinessType);

                if (!string.IsNullOrEmpty(request.Description))
                    category.SetDescription(request.Description);

                if (!string.IsNullOrEmpty(request.IconUrl))
                    category.SetIconUrl(request.IconUrl);

                if (!string.IsNullOrEmpty(request.ImageUrl))
                    category.SetImageUrl(request.ImageUrl);

                if (parentCategory is not null)
                    category.SetParentCategory(parentCategory);

                // 4. Kaydet
                await _categoryRepository.AddAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(category.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kategori oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}