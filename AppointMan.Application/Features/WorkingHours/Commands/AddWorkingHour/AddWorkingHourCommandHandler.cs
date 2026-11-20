using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Commands.AddWorkingHour
{
    public class AddWorkingHourCommandHandler : IRequestHandler<AddWorkingHourCommand, Result<Guid>>
    {
        private readonly IRepository<WorkingHour> _workingHourRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public AddWorkingHourCommandHandler(
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

        public async Task<Result<Guid>> Handle(AddWorkingHourCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu: govde OwnerId'si degil, JWT kimligi esas alinir.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu isletmeye calisma saati ekleme yetkiniz yok."));

                // 3. Ayni gun icin calisma saati var mi?
                var existing = await _workingHourRepository.FindAsync(
                    w => w.BusinessId == request.BusinessId && w.DayOfWeek == request.DayOfWeek && !w.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict($"{request.DayOfWeek} gunu icin zaten calisma saati tanimli."));

                // 4. Calisma saati olustur
                var workingHour = new WorkingHour(
                    request.DayOfWeek,
                    request.StartTime,
                    request.EndTime,
                    request.BusinessId);

                // 5. Kapali ise isaretle
                if (request.IsClosed)
                    workingHour.Close();

                // 6. Kaydet
                await _workingHourRepository.AddAsync(workingHour, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(workingHour.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Calisma saati eklenirken bir hata olustu.", ex.Message));
            }
        }
    }
}