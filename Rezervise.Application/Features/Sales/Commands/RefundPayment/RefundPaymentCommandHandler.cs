using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Sales.Commands.RefundPayment
{
    public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, Result<Guid>>
    {
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTimeService;

        public RefundPaymentCommandHandler(
            IRepository<Payment> paymentRepository,
            IRepository<Business> businessRepository,
            IRepository<CashSession> cashSessionRepository,
            IRepository<CashMovement> cashMovementRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTimeService)
        {
            _paymentRepository = paymentRepository;
            _businessRepository = businessRepository;
            _cashSessionRepository = cashSessionRepository;
            _cashMovementRepository = cashMovementRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Orijinal ödemeyi bul
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
                if (payment is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Ödeme", request.PaymentId));

                // 2. Yetki kontrolü: ödemenin işletmesi üzerinden business.OwnerId esas alınır.
                var business = await _businessRepository.GetByIdAsync(payment.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("İşletme", payment.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletme için iade işlemi yapma yetkiniz yok."));

                // 3. Ters kayıt ilkesi: iade kaydı tekrar iade edilemez.
                if (payment.IsRefund)
                    return Result<Guid>.Failure(Error.BusinessRule("İade kaydı tekrar iade edilemez."));

                // 4. Tutar doğrulama: kalan iade tutarı aşılamaz.
                if (request.Amount <= 0)
                    return Result<Guid>.Failure(Error.Validation("İade tutarı sıfırdan büyük olmalıdır."));

                var previousRefunds = await _paymentRepository.FindAsync(
                    p => p.RefersToPaymentId == payment.Id && !p.IsDeleted,
                    cancellationToken);

                var alreadyRefunded = previousRefunds.Sum(p => p.Amount);
                var remaining = payment.Amount - alreadyRefunded;

                if (request.Amount > remaining)
                    return Result<Guid>.Failure(Error.Validation(
                        "İade tutarı orijinal ödemenin kalan iade tutarını geçemez.",
                        $"Kalan iade tutarı: {remaining}"));

                // 5. Nakit iadeler için açık kasa oturumu zorunludur; iade hem
                //    ters ödeme kaydı hem de kasaya CashOut hareketi olarak yazılır.
                var now = _dateTimeService.UtcNow;
                Guid? cashSessionId = null;

                if (payment.PaymentMethod == EnumPaymentMethod.Cash)
                {
                    var openSessions = await _cashSessionRepository.FindAsync(
                        s => s.BusinessId == payment.BusinessId &&
                             s.Status == EnumCashSessionStatus.Open &&
                             !s.IsDeleted,
                        cancellationToken);

                    var openSession = openSessions.FirstOrDefault();

                    if (openSession is null)
                        return Result<Guid>.Failure(Error.Conflict(
                            "Nakit iade için açık kasa oturumu bulunamadı."));

                    cashSessionId = openSession.Id;
                }

                // 6. Ters kaydı oluştur (negatif tutar yerine IsRefund bayraklı kayıt)
                var refundPayment = Payment.CreateRefund(payment, request.Amount, now, cashSessionId);
                await _paymentRepository.AddAsync(refundPayment, cancellationToken);

                // 7. Nakit ise kasadan çıkış hareketi yaz. ReferenceId dolu öldüğü
                //    için gün sonu hesabında manuel çıkışlara değil iade ödemesine
                //    bağlı olarak sayılır (çifte sayım önlenir).
                if (payment.PaymentMethod == EnumPaymentMethod.Cash && cashSessionId.HasValue)
                {
                    var movement = new CashMovement(
                        payment.BusinessId,
                        cashSessionId.Value,
                        EnumCashMovementType.CashOut,
                        request.Amount,
                        request.Reason ?? "Nakit iade",
                        refundPayment.Id,
                        _currentUser.UserId!.Value);

                    await _cashMovementRepository.AddAsync(movement, cancellationToken);
                }

                // 8. Ödemenin tamamı iade edildiyse orijinal kaydı işaretle.
                if (alreadyRefunded + request.Amount >= payment.Amount)
                    payment.MarkRefunded(now);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(refundPayment.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("İade işlemi sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
