using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.RecordStockMovement
{
    public class RecordStockMovementCommandHandler : IRequestHandler<RecordStockMovementCommand, Result<Guid>>
    {
        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public RecordStockMovementCommandHandler(
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Product> productRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _stockMovementRepository = stockMovementRepository;
            _productRepository = productRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<Guid>> Handle(RecordStockMovementCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmeye stok hareketi kaydetme yetkiniz yok."));

                // 3. Ürün kontrolü: ürün işletmeye ait olmalı.
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || product.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Ürün", request.ProductId.ToString()));

                // 4. Çift işleme engeli: aynı (ReferenceType, ReferenceId)
                //    referansıyla ürün bazında hareket zaten işlenmişse reddet.
                if (!string.IsNullOrWhiteSpace(request.ReferenceType) && request.ReferenceId.HasValue)
                {
                    var duplicate = await _stockMovementRepository.FindAsync(
                        m => m.BusinessId == request.BusinessId
                             && m.ProductId == request.ProductId
                             && m.ReferenceType == request.ReferenceType
                             && m.ReferenceId == request.ReferenceId.Value,
                        cancellationToken);

                    if (duplicate.Any())
                        return Result<Guid>.Failure(Error.Conflict(
                            "Bu referans için stok hareketi zaten işlenmiştir. Aynı hareket iki kez kaydedilemez."));
                }

                // 5. Negatif stok politikası: mevcut bakiye hareket toplamıdır;
                //    çıkış hareketi bakiyeyi eksiye düşürüyorsa reddet.
                var movements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == request.BusinessId && m.ProductId == request.ProductId && !m.IsDeleted,
                    cancellationToken);

                var balance = movements.Sum(m => m.Quantity);
                var signedQuantity = IsIncoming(request.MovementType) ? request.Quantity : -request.Quantity;

                if (balance + signedQuantity < 0)
                    return Result<Guid>.Failure(Error.Conflict(
                        $"Stok bakiyesi yetersiz. Mevcut bakiye: {balance}, talep edilen çıkış: {request.Quantity}.",
                        $"UrunId: {request.ProductId}"));

                // 6. Hareketi oluştur (işareti entity belirler)
                var movement = new StockMovement(
                    request.BusinessId,
                    request.ProductId,
                    request.MovementType,
                    request.Quantity,
                    request.UnitCost ?? 0m,
                    request.OccurredAt ?? _dateTime.UtcNow,
                    request.ReferenceType,
                    request.ReferenceId);

                // 7. Kaydet
                await _stockMovementRepository.AddAsync(movement, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(movement.Id);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanlı iki işleme denemesi: referans unique index'i ikincisini reddeder.
                return Result<Guid>.Failure(Error.Conflict(
                    "Bu referans için stok hareketi zaten işlenmiştir. Aynı hareket iki kez kaydedilemez."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Stok hareketi kaydedilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>Giriş hareketleri bakiyeyi artırır; işaret entity ile aynı kurala göre.</summary>
        private static bool IsIncoming(EnumStockMovementType movementType)
            => movementType is EnumStockMovementType.PurchaseIn
                or EnumStockMovementType.CustomerReturn;
    }
}
