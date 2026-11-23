using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Commands.AddWorkingHour
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
                // 1. İşletme kontrolü
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolü: gövde OwnerId'si değil, JWT kimliği esas alınır.
                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Unauthorized("Bu işletmeye çalışma saati ekleme yetkiniz yok."));

                // 3. Aynı gün için çalışma saati var mi?
                var existing = await _workingHourRepository.FindAsync(
                    w => w.BusinessId == request.BusinessId && w.DayOfWeek == request.DayOfWeek && !w.IsDeleted,
                    cancellationToken);

                if (existing.Any())
                    return Result<Guid>.Failure(Error.Conflict($"{request.DayOfWeek} günü için zaten çalışma saati tanımlı."));

                // 4. Çalışma saati oluştur
                var workingHour = new WorkingHour(
                    request.DayOfWeek,
                    request.StartTime,
                    request.EndTime,
                    request.BusinessId);

                // 5. Kapalı ise işaretle
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
                return Result<Guid>.Failure(Error.Internal("Çalışma saati eklenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}