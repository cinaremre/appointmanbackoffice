using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Inventory.Commands.ConsumeServiceMaterials
{
    public class ConsumeServiceMaterialsCommandHandler : IRequestHandler<ConsumeServiceMaterialsCommand, Result<int>>
    {
        private const string AppointmentReferenceType = "Appointment";

        private readonly IRepository<StockMovement> _stockMovementRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTime;

        public ConsumeServiceMaterialsCommandHandler(
            IRepository<StockMovement> stockMovementRepository,
            IRepository<Product> productRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTime)
        {
            _stockMovementRepository = stockMovementRepository;
            _productRepository = productRepository;
            _appointmentRepository = appointmentRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTime = dateTime;
        }

        public async Task<Result<int>> Handle(ConsumeServiceMaterialsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<int>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<int>.Failure(Error.Forbidden("Bu isletmeye malzeme tuketimi isleme yetkiniz yok."));

                // 3. Tuketim listesi bos olamaz.
                if (request.Materials is null || request.Materials.Count == 0)
                    return Result<int>.Failure(Error.Validation("Tuketim listesi bos olamaz."));

                // 4. Randevu kontrolu: randevu isletmeye ait ve Completed olmali.
                var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
                if (appointment is null || appointment.BusinessId != request.BusinessId)
                    return Result<int>.Failure(Error.NotFound("Randevu", request.AppointmentId.ToString()));

                if (appointment.Status != EnumAppointmentStatus.Completed)
                    return Result<int>.Failure(Error.BusinessRule(
                        "Malzeme tuketimi yalnizca tamamlanmis (Completed) randevular icin islenebilir."));

                // 5. Cift isleme engeli: ayni randevu icin tuketim zaten islenmis mi?
                var appointmentMovements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == request.BusinessId
                         && m.ReferenceType == AppointmentReferenceType
                         && m.ReferenceId == request.AppointmentId,
                    cancellationToken);

                if (appointmentMovements.Any())
                    return Result<int>.Failure(Error.Conflict(
                        "Bu randevu icin malzeme tuketimi zaten islenmistir. Ayni tuketim iki kez kaydedilemez."));

                // 6. Satirlari urun bazinda birlestir (ayni urunden iki satir gelirse tek harekette toplanir).
                var quantitiesByProduct = request.Materials
                    .GroupBy(l => l.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

                if (quantitiesByProduct.Any(kv => kv.Value <= 0))
                    return Result<int>.Failure(Error.Validation("Tuketim miktari sifirdan buyuk olmalidir."));

                // 7. Urunleri kontrol et: hepsi isletmeye ait olmali.
                var missingProducts = new List<Guid>();
                foreach (var productId in quantitiesByProduct.Keys)
                {
                    var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
                    if (product is null || product.BusinessId != request.BusinessId)
                        missingProducts.Add(productId);
                }

                if (missingProducts.Count > 0)
                    return Result<int>.Failure(Error.NotFound("Urun", string.Join(", ", missingProducts)));

                // 8. Negatif stok politikasi: urun bazinda mevcut bakiyeyi asan tuketim reddedilir.
                var businessMovements = await _stockMovementRepository.FindAsync(
                    m => m.BusinessId == request.BusinessId && !m.IsDeleted,
                    cancellationToken);

                var balances = businessMovements
                    .GroupBy(m => m.ProductId)
                    .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

                foreach (var (productId, consumed) in quantitiesByProduct)
                {
                    var balance = balances.GetValueOrDefault(productId, 0m);
                    if (balance - consumed < 0)
                        return Result<int>.Failure(Error.Conflict(
                            $"Stok bakiyesi yetersiz. UrunId: {productId}, mevcut bakiye: {balance}, tuketim: {consumed}."));
                }

                // 9. ServiceConsumption hareketlerini olustur.
                var occurredAt = _dateTime.UtcNow;
                var movements = quantitiesByProduct
                    .Select(kv => new StockMovement(
                        request.BusinessId,
                        kv.Key,
                        EnumStockMovementType.ServiceConsumption,
                        kv.Value,
                        0m,
                        occurredAt,
                        AppointmentReferenceType,
                        request.AppointmentId))
                    .ToList();

                foreach (var movement in movements)
                    await _stockMovementRepository.AddAsync(movement, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<int>.Success(movements.Count);
            }
            catch (DuplicateKeyException)
            {
                // Es zamanli iki isleme denemesi: referans unique index'i ikincisini reddeder.
                return Result<int>.Failure(Error.Conflict(
                    "Bu randevu icin malzeme tuketimi zaten islenmistir. Ayni tuketim iki kez kaydedilemez."));
            }
            catch (DomainException ex)
            {
                return Result<int>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(Error.Internal("Malzeme tuketimi islenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
