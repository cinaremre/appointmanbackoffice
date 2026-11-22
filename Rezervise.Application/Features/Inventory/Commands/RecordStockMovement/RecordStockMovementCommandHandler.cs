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
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmeye stok hareketi kaydetme yetkiniz yok."));

                // 3. Urun kontrolu: urun isletmeye ait olmali.
                var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
                if (product is null || product.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Urun", request.ProductId.ToString()));

                // 4. Cift isleme engeli: ayni (ReferenceType, ReferenceId)
                //    referansiyla urun bazinda hareket zaten islenmisse reddet.
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
                            "Bu referans icin stok hareketi zaten islenmistir. Ayni hareket iki kez kaydedilemez."));
                }

                // 5. Negatif stok politikasi: mevcut bakiye hareket toplamidir;
                //    cikis hareketi bakiyeyi eksiye dusuruyorsa reddet.
                var movements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == request.BusinessId && m.ProductId == request.ProductId && !m.IsDeleted,
                    cancellationToken);

                var balance = movements.Sum(m => m.Quantity);
                var signedQuantity = IsIncoming(request.MovementType) ? request.Quantity : -request.Quantity;

                if (balance + signedQuantity < 0)
                    return Result<Guid>.Failure(Error.Conflict(
                        $"Stok bakiyesi yetersiz. Mevcut bakiye: {balance}, talep edilen cikis: {request.Quantity}.",
                        $"UrunId: {request.ProductId}"));

                // 6. Hareketi olustur (isareti entity belirler)
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
                // Es zamanli iki isleme denemesi: referans unique index'i ikincisini reddeder.
                return Result<Guid>.Failure(Error.Conflict(
                    "Bu referans icin stok hareketi zaten islenmistir. Ayni hareket iki kez kaydedilemez."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Stok hareketi kaydedilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>Giris hareketleri bakiyeyi artirir; isaret entity ile ayni kurala gore.</summary>
        private static bool IsIncoming(EnumStockMovementType movementType)
            => movementType is EnumStockMovementType.PurchaseIn
                or EnumStockMovementType.CustomerReturn;
    }
}
