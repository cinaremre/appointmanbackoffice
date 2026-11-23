using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.CreateProduct
{
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public CreateProductCommandHandler(
            IRepository<Product> productRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _productRepository = productRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmeye ürün tanımlama yetkiniz yok."));

                // 3. SKU işletme bazında benzersiz olmalı (elle kontrol;
                //    es zamanlı iki isteği unique index yakalar).
                if (string.IsNullOrWhiteSpace(request.Sku))
                    return Result<Guid>.Failure(Error.BusinessRule("Ürün SKU'su boş olamaz."));

                var existing = await _productRepository.FindAsync(
                    p => p.BusinessId == request.BusinessId && p.Sku == request.Sku.Trim().ToUpperInvariant() && !p.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict($"'{request.Sku}' SKU'su ile bir ürün zaten tanımlı."));

                // 4. Ürünü oluştur (entity is kurallarını uygular)
                var product = new Product(
                    request.Sku,
                    request.Name,
                    business.Id,
                    request.PurchasePrice,
                    request.SalePrice,
                    request.MinimumStock,
                    request.Unit,
                    request.Barcode,
                    request.CategoryId);

                // 5. Kaydet
                await _productRepository.AddAsync(product, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(product.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanlı iki ekleme denemesi: unique index ikincisini reddeder.
                return Result<Guid>.Failure(
                    Error.Conflict($"'{request.Sku}' SKU'su ile bir ürün zaten tanımlı."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Ürün oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
