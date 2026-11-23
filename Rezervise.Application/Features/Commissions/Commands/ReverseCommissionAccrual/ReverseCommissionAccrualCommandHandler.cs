using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Commissions.Commands.ReverseCommissionAccrual
{
    public class ReverseCommissionAccrualCommandHandler : IRequestHandler<ReverseCommissionAccrualCommand, Result<Guid>>
    {
        private readonly IRepository<CommissionAccrual> _accrualRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public ReverseCommissionAccrualCommandHandler(
            IRepository<CommissionAccrual> accrualRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _accrualRepository = accrualRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(ReverseCommissionAccrualCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu işletmenin prim tahakkukunu ters çevirme yetkiniz yok."));

                // 3. Tahakkuk kontrolü: kayıt işletmeye ait olmalı.
                var accrual = await _accrualRepository.GetByIdAsync(request.AccrualId, cancellationToken);
                if (accrual is null || accrual.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Prim tahakkuku", request.AccrualId.ToString()));

                // 4. Ters çevir: Accrued -> Reversed. Paid veya Reversed durumlarında
                //    entity DomainException fırlatır ve aşağıdaki catch reddeder.
                accrual.Reverse();

                // 5. Kaydet
                await _accrualRepository.UpdateAsync(accrual, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(accrual.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Prim tahakkuku ters çevrilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
