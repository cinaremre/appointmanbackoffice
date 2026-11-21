using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Cash.Commands.AddCashMovement
{
    public class AddCashMovementCommandHandler : IRequestHandler<AddCashMovementCommand, Result<Guid>>
    {
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<CashMovement> _cashMovementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AddCashMovementCommandHandler(
            IRepository<CashSession> cashSessionRepository,
            IRepository<Business> businessRepository,
            IRepository<CashMovement> cashMovementRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _cashSessionRepository = cashSessionRepository;
            _businessRepository = businessRepository;
            _cashMovementRepository = cashMovementRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(AddCashMovementCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Oturum kontrolu
                var session = await _cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
                if (session is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Kasa oturumu", request.CashSessionId));

                // 2. Yetki kontrolu
                var business = await _businessRepository.GetByIdAsync(session.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Isletme", session.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu kasa oturumuna hareket ekleme yetkiniz yok."));

                // 3. Hareket yalnizca acik oturuma eklenebilir
                if (session.Status != EnumCashSessionStatus.Open)
                    return Result<Guid>.Failure(
                        Error.BusinessRule("Kasa hareketi yalnizca acik oturuma eklenebilir."));

                // 4. Tutar pozitif olmali
                if (request.Amount <= 0)
                    return Result<Guid>.Failure(Error.Validation("Kasa hareket tutari sifirdan buyuk olmalidir."));

                // 5. Hareketi olustur (manuel hareket: ReferenceId bos)
                var movement = new CashMovement(
                    session.BusinessId,
                    session.Id,
                    request.MovementType,
                    request.Amount,
                    request.Reason,
                    null,
                    _currentUser.UserId!.Value);

                await _cashMovementRepository.AddAsync(movement, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(movement.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kasa hareketi eklenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
