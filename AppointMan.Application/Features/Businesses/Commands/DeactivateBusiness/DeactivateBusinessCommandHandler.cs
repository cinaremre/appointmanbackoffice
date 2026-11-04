using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Businesses.Commands.DeactivateBusiness
{
    public class DeactivateBusinessCommandHandler : IRequestHandler<DeactivateBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeactivateBusinessCommandHandler(
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(DeactivateBusinessCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu - Isletme sahibi mi?
                if (business.OwnerId != request.OwnerId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmeyi pasif etme yetkiniz yok."));

                // 3. Zaten pasif mi?
                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Isletme zaten pasif."));

                // 4. Pasif et
                business.Deactivate(request.DeactivatedBy, request.Reason);

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(business.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Isletme pasif edilirken bir hata olustu.", ex.Message));
            }
        }
    }
}