using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Commands.RemoveWorkingHour
{
    public class RemoveWorkingHourCommandHandler : IRequestHandler<RemoveWorkingHourCommand, Result<Guid>>
    {
        private readonly IRepository<WorkingHour> _workingHourRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public RemoveWorkingHourCommandHandler(
            IRepository<WorkingHour> workingHourRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _workingHourRepository = workingHourRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(RemoveWorkingHourCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu calisma saatini silme yetkiniz yok."));

                // 3. Calisma saati kontrolu
                var workingHour = await _workingHourRepository.GetByIdAsync(request.WorkingHourId, cancellationToken);
                if (workingHour is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisma saati", request.WorkingHourId.ToString()));

                // 4. Calisma saati isletmeye ait mi?
                if (workingHour.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisma saati bu isletmeye ait degil."));

                // 5. Sil
                workingHour.Delete();

                // 6. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(workingHour.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Calisma saati silinirken bir hata olustu.", ex.Message));
            }
        }
    }
}