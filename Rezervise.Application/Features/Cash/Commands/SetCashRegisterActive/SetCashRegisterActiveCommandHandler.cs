using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.SetCashRegisterActive
{
    public class SetCashRegisterActiveCommandHandler
        : IRequestHandler<SetCashRegisterActiveCommand, Result<Guid>>
    {
        private readonly IRepository<CashRegister> _cashRegisterRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetCashRegisterActiveCommandHandler(
            IRepository<CashRegister> cashRegisterRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _cashRegisterRepository = cashRegisterRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(SetCashRegisterActiveCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kasa kontrolu
                var register = await _cashRegisterRepository.GetByIdAsync(request.CashRegisterId, cancellationToken);
                if (register is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Kasa", request.CashRegisterId));

                // 2. Yetki kontrolu: kasanin isletmesi uzerinden business.OwnerId esas alinir.
                var business = await _businessRepository.GetByIdAsync(register.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Isletme", register.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu kasayi degistirme yetkiniz yok."));

                // 3. Toggle: hedef duruma gec (ayni durumdaysa dokunma).
                if (request.IsActive)
                    register.Reactivate();
                else
                    register.Deactivate();

                await _cashRegisterRepository.UpdateAsync(register, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(register.Id);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kasa durumu degistirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
