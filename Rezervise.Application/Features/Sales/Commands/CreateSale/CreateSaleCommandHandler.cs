using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Sales.Commands.CreateSale
{
    public class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly IRepository<Sale> _saleRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTimeService;

        public CreateSaleCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            IRepository<Sale> saleRepository,
            IRepository<Payment> paymentRepository,
            IRepository<CashSession> cashSessionRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTimeService)
        {
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _saleRepository = saleRepository;
            _paymentRepository = paymentRepository;
            _cashSessionRepository = cashSessionRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Isletme", request.BusinessId));

                // 2. Yetki kontrolu: govde degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletme icin satis islemi yapma yetkiniz yok."));

                // 3. Satir listesi bos olamaz
                if (request.Lines is null || request.Lines.Count == 0)
                    return Result<Guid>.Failure(Error.Validation("Satis en az bir satir icermelidir."));

                // 4. Randevu kontrolu: satis yalnizca tamamlanmis randevuya baglanabilir.
                if (request.AppointmentId.HasValue)
                {
                    var appointment = await _appointmentRepository.GetByIdAsync(
                        request.AppointmentId.Value, cancellationToken);

                    if (appointment is null)
                        return Result<Guid>.Failure(Error.NotFoundById("Randevu", request.AppointmentId.Value));

                    if (appointment.BusinessId != request.BusinessId)
                        return Result<Guid>.Failure(Error.Validation("Randevu bu isletmeye ait degil."));

                    if (appointment.Status != EnumAppointmentStatus.Completed)
                        return Result<Guid>.Failure(
                            Error.BusinessRule("Satis yalnizca tamamlanmis bir randevuya baglanabilir."));
                }

                // 5. Satisi kur; satir toplami = Quantity * UnitPrice - Discount + Tax.
                //    Sale aggregate'i her satir eklendiginde toplamlari yeniden hesaplar:
                //    GrandTotal = Subtotal - DiscountTotal + TaxTotal.
                var now = _dateTimeService.UtcNow;
                var currency = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency;

                var sale = new Sale(request.BusinessId, now, currency,
                    request.AppointmentId, request.CustomerId);

                foreach (var line in request.Lines)
                {
                    sale.AddLine(new SaleLine(
                        sale.Id,
                        line.ItemType,
                        line.ServiceId,
                        line.ProductId,
                        line.Quantity,
                        line.UnitPrice,
                        line.DiscountAmount,
                        line.TaxAmount,
                        line.EmployeeId));
                }

                // 6. Siparis seviyesi indirim
                if (request.DiscountTotal > 0)
                    sale.ApplyOrderDiscount(request.DiscountTotal);

                // 7. Odeme listesi bos olamaz; odeme toplami GrandTotal'i gecemez.
                if (request.Payments is null || request.Payments.Count == 0)
                    return Result<Guid>.Failure(Error.Validation("Satis icin en az bir odeme gereklidir."));

                foreach (var payment in request.Payments)
                {
                    if (payment.Amount <= 0)
                        return Result<Guid>.Failure(Error.Validation("Odeme tutari sifirdan buyuk olmalidir."));
                }

                var paymentTotal = Math.Round(request.Payments.Sum(p => p.Amount), 2);

                if (paymentTotal > sale.GrandTotal)
                    return Result<Guid>.Failure(Error.Validation(
                        "Odeme toplami satis tutarini gecemez.",
                        $"Odeme: {paymentTotal}, Satis tutari: {sale.GrandTotal}"));

                // 8. Ilk surumde satis tek adimda tamamlanir.
                sale.Complete(now);

                // 9. Acik kasa oturumu: nakit odemeler oturuma baglanir ki
                //    gun sonu beklenen nakit hesabina dahil edilsin.
                var openSessions = await _cashSessionRepository.FindAsync(
                    s => s.BusinessId == request.BusinessId &&
                         s.Status == EnumCashSessionStatus.Open &&
                         !s.IsDeleted,
                    cancellationToken);

                var openSession = openSessions.FirstOrDefault();

                // 10. Odeme kayitlarini olustur
                foreach (var paymentInput in request.Payments)
                {
                    var cashSessionId = paymentInput.PaymentMethod == EnumPaymentMethod.Cash
                        ? openSession?.Id
                        : null;

                    var payment = new Payment(
                        request.BusinessId,
                        sale.Id,
                        paymentInput.Amount,
                        paymentInput.PaymentMethod,
                        now,
                        cashSessionId,
                        currency);

                    await _paymentRepository.AddAsync(payment, cancellationToken);
                }

                // 11. Kaydet
                await _saleRepository.AddAsync(sale, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(sale.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Satis olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
