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
                // 1. Orijinal odemeyi bul
                var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
                if (payment is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Odeme", request.PaymentId));

                // 2. Yetki kontrolu: odemenin isletmesi uzerinden business.OwnerId esas alinir.
                var business = await _businessRepository.GetByIdAsync(payment.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Isletme", payment.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletme icin iade islemi yapma yetkiniz yok."));

                // 3. Ters kayit ilkesi: iade kaydi tekrar iade edilemez.
                if (payment.IsRefund)
                    return Result<Guid>.Failure(Error.BusinessRule("Iade kaydi tekrar iade edilemez."));

                // 4. Tutar dogrulama: kalan iade tutari asilamaz.
                if (request.Amount <= 0)
                    return Result<Guid>.Failure(Error.Validation("Iade tutari sifirdan buyuk olmalidir."));

                var previousRefunds = await _paymentRepository.FindAsync(
                    p => p.RefersToPaymentId == payment.Id && !p.IsDeleted,
                    cancellationToken);

                var alreadyRefunded = previousRefunds.Sum(p => p.Amount);
                var remaining = payment.Amount - alreadyRefunded;

                if (request.Amount > remaining)
                    return Result<Guid>.Failure(Error.Validation(
                        "Iade tutari orijinal odemenin kalan iade tutarini gecemez.",
                        $"Kalan iade tutari: {remaining}"));

                // 5. Nakit iadeler icin acik kasa oturumu zorunludur; iade hem
                //    ters odeme kaydi hem de kasaya CashOut hareketi olarak yazilir.
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
                            "Nakit iade icin acik kasa oturumu bulunamadi."));

                    cashSessionId = openSession.Id;
                }

                // 6. Ters kaydi olustur (negatif tutar yerine IsRefund bayrakli kayit)
                var refundPayment = Payment.CreateRefund(payment, request.Amount, now, cashSessionId);
                await _paymentRepository.AddAsync(refundPayment, cancellationToken);

                // 7. Nakit ise kasadan cikis hareketi yaz. ReferenceId dolu oldugu
                //    icin gun sonu hesabinda manuel cikislara degil iade odemesine
                //    bagli olarak sayilir (cifte sayim onlenir).
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

                // 8. Odemenin tamami iade edildiyse orijinal kaydi isaretle.
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
                return Result<Guid>.Failure(Error.Internal("Iade islemi sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
