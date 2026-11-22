using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.CloseCashSession
{
    public class CloseCashSessionCommandHandler
        : IRequestHandler<CloseCashSessionCommand, Result<CashSessionCloseResult>>
    {
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IRepository<Payment> _paymentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTimeService;

        public CloseCashSessionCommandHandler(
            IRepository<CashSession> cashSessionRepository,
            IRepository<Business> businessRepository,
            IRepository<CashMovement> cashMovementRepository,
            IRepository<Payment> paymentRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTimeService)
        {
            _cashSessionRepository = cashSessionRepository;
            _businessRepository = businessRepository;
            _cashMovementRepository = cashMovementRepository;
            _paymentRepository = paymentRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<CashSessionCloseResult>> Handle(
            CloseCashSessionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Oturum kontrolu
                var session = await _cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
                if (session is null)
                    return Result<CashSessionCloseResult>.Failure(
                        Error.NotFoundById("Kasa oturumu", request.CashSessionId));

                // 2. Yetki kontrolu
                var business = await _businessRepository.GetByIdAsync(session.BusinessId, cancellationToken);
                if (business is null)
                    return Result<CashSessionCloseResult>.Failure(
                        Error.NotFoundById("Isletme", session.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<CashSessionCloseResult>.Failure(
                        Error.Forbidden("Bu kasa oturumunu kapatma yetkiniz yok."));

                // 3. Oturum acik olmali
                if (session.Status != EnumCashSessionStatus.Open)
                    return Result<CashSessionCloseResult>.Failure(
                        Error.Conflict("Kasa oturumu zaten kapali."));

                // 4. Manuel giris/cikis toplamlari. ReferenceId dolu hareketler
                //    bir odeme kaydindan turemistir (nakit iade ters kaydi);
                //    bunlar asagida iade odemeleri uzerinden sayilir.
                var movements = await _cashMovementRepository.FindAsync(
                    m => m.CashSessionId == session.Id && !m.IsDeleted,
                    cancellationToken);

                var totalCashIn = movements
                    .Where(m => m.MovementType == EnumCashMovementType.CashIn)
                    .Sum(m => m.Amount);

                var totalCashOut = movements
                    .Where(m => m.MovementType == EnumCashMovementType.CashOut && m.ReferenceId == null)
                    .Sum(m => m.Amount);

                // 5. Oturuma bagli odemeler: nakit tahsilat ve nakit iade.
                //    Kart tahsilatlar beklenen nakide dahil DEGILDIR.
                var payments = await _paymentRepository.FindAsync(
                    p => p.CashSessionId == session.Id && !p.IsDeleted,
                    cancellationToken);

                var totalCashCollected = payments
                    .Where(p => !p.IsRefund && p.PaymentMethod == EnumPaymentMethod.Cash)
                    .Sum(p => p.Amount);

                var totalCashRefunded = payments
                    .Where(p => p.IsRefund && p.PaymentMethod == EnumPaymentMethod.Cash)
                    .Sum(p => p.Amount);

                // 6. Oturumu kapat; farki (sayilan - beklenen) rapola.
                var now = _dateTimeService.UtcNow;
                session.Close(
                    request.CountedCash,
                    totalCashCollected,
                    totalCashIn,
                    totalCashOut,
                    totalCashRefunded,
                    _currentUser.UserId!.Value,
                    now);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<CashSessionCloseResult>.Success(new CashSessionCloseResult
                {
                    CashSessionId = session.Id,
                    ExpectedCash = session.ExpectedCash!.Value,
                    CountedCash = session.CountedCash!.Value,
                    Difference = session.Difference!.Value
                });
            }
            catch (DomainException ex)
            {
                return Result<CashSessionCloseResult>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<CashSessionCloseResult>.Failure(
                    Error.Internal("Kasa oturumu kapatilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
