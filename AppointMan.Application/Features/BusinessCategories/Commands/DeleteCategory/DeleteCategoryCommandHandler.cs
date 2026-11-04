using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.BusinessCategories.Commands.DeleteCategory
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCategoryCommandHandler(
            IRepository<BusinessCategory> categoryRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kategori kontrolu
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
                if (category is null)
                    return Result<Guid>.Failure(Error.NotFound("Kategori", request.CategoryId.ToString()));

                // 2. Alt kategori var mi?
                var subCategories = await _categoryRepository.FindAsync(
                    c => c.ParentCategoryId == request.CategoryId && !c.IsDeleted,
                    cancellationToken);

                if (subCategories.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu kategorinin alt kategorileri var. Once onlari silin."));

                // 3. Bu kategoriye bagli isletme var mi?
                // Business tablosunda CategoryId ile iliskili kayitlar kontrol edilebilir
                // Burada Business repository kullanilarak kontrol yapilabilir

                // 4. Kategoriyi sil
                category.SoftDelete();

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
                return Result<Guid>.Failure(Error.Internal("Kategori silinirken bir hata olustu.", ex.Message));
            }
        }
    }
}