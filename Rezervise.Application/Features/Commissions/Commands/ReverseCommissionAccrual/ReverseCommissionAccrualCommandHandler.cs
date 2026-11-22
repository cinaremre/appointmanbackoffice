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
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu isletmenin prim tahakkukunu ters cevirme yetkiniz yok."));

                // 3. Tahakkuk kontrolu: kayit isletmeye ait olmali.
                var accrual = await _accrualRepository.GetByIdAsync(request.AccrualId, cancellationToken);
                if (accrual is null || accrual.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.NotFound("Prim tahakkuku", request.AccrualId.ToString()));

                // 4. Ters cevir: Accrued -> Reversed. Paid veya Reversed durumlarinda
                //    entity DomainException firlatir ve asagidaki catch reddeder.
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
                return Result<Guid>.Failure(Error.Internal("Prim tahakkuku ters cevrilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
