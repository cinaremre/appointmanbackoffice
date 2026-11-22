using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Commands.SetClosed
{
    public class SetClosedCommandHandler : IRequestHandler<SetClosedCommand, Result<Guid>>
    {
        private readonly IRepository<WorkingHour> _workingHourRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public SetClosedCommandHandler(
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

        public async Task<Result<Guid>> Handle(SetClosedCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu islemi yapma yetkiniz yok."));

                // 3. Calisma saati kontrolu
                var workingHour = await _workingHourRepository.GetByIdAsync(request.WorkingHourId, cancellationToken);
                if (workingHour is null)
                    return Result<Guid>.Failure(Error.NotFound("Calisma saati", request.WorkingHourId.ToString()));

                // 4. Calisma saati isletmeye ait mi?
                if (workingHour.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Calisma saati bu isletmeye ait degil."));

                // 5. Kapali olarak isaretle
                workingHour.Close();

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
                return Result<Guid>.Failure(Error.Internal("Islem yapilirken bir hata olustu.", ex.Message));
            }
        }
    }
}