using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.BusinessCategories.Commands.UpdateCategory
{
    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCategoryCommandHandler(
            IRepository<BusinessCategory> categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kategori kontrolu
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null)
                    return Result<Guid>.Failure(Error.NotFound("Kategori", request.CategoryId.ToString()));

                // 2. Ayni isimde baska kategori var mi?
                var existingCategories = await _categoryRepository.FindAsync(
                    c => c.Name == request.Name && c.Id != request.CategoryId && !c.IsDeleted,
                    cancellationToken);

                if (existingCategories.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir kategori zaten mevcut."));

                // 3. Parent kategori kontrolu
                BusinessCategory? parentCategory = null;
                if (request.ParentCategoryId.HasValue)
                {
                    if (request.ParentCategoryId.Value == request.CategoryId)
                        return Result<Guid>.Failure(Error.BusinessRule("Kategori kendisinin ust kategorisi olamaz."));

                    parentCategory = await _categoryRepository.GetByIdAsync(
                        request.ParentCategoryId.Value, cancellationToken);

                    if (parentCategory is null)
                        return Result<Guid>.Failure(Error.NotFound("Ust kategori", request.ParentCategoryId.Value.ToString()));
                }

                // 4. Guncelle
                category.SetName(request.Name);

                if (!string.IsNullOrEmpty(request.Description))
                    category.SetDescription(request.Description);

                if (!string.IsNullOrEmpty(request.IconUrl))
                    category.SetIconUrl(request.IconUrl);

                if (!string.IsNullOrEmpty(request.ImageUrl))
                    category.SetImageUrl(request.ImageUrl);

                category.SetDisplayOrder(request.DisplayOrder);

                if (request.IsActive)
                    category.Activate();
                else
                    category.Deactivate();

                if (request.CorrespondingBusinessType.HasValue)
                {
                    // BusinessType guncelleme
                }

                if (parentCategory is not null)
                    category.SetParentCategory(parentCategory);
                else
                    category.SetParentCategory(null);

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(category.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kategori guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}