using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.AddCashMovement
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
                // 1. Oturum kontrolü
                var session = await _cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
                if (session is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Kasa oturumu", request.CashSessionId));

                // 2. Yetki kontrolü
                var business = await _businessRepository.GetByIdAsync(session.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("İşletme", session.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu kasa oturumuna hareket ekleme yetkiniz yok."));

                // 3. Hareket yalnızca açık oturuma eklenebilir
                if (session.Status != EnumCashSessionStatus.Open)
                    return Result<Guid>.Failure(
                        Error.BusinessRule("Kasa hareketi yalnızca açık oturuma eklenebilir."));

                // 4. Tutar pozitif olmalı
                if (request.Amount <= 0)
                    return Result<Guid>.Failure(Error.Validation("Kasa hareket tutarı sıfırdan büyük olmalıdır."));

                // 5. Hareketi oluştur (manuel hareket: ReferenceId boş)
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
                return Result<Guid>.Failure(Error.Internal("Kasa hareketi eklenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
