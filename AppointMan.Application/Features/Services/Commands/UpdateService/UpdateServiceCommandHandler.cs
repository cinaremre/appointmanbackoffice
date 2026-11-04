using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using AppointMan.Domain.ValueObjects;
using MediatR;

namespace AppointMan.Application.Features.Services.Commands.UpdateService
{
    public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, Result<Guid>>
    {
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateServiceCommandHandler(
            IRepository<Service> serviceRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork)
        {
            _serviceRepository = serviceRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme kontrolu
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 2. Yetki kontrolu - Isletme sahibi mi?
                if (business.OwnerId != request.OwnerId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu hizmeti guncelleme yetkiniz yok."));

                // 3. Hizmet kontrolu
                var service = await _serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
                if (service is null)
                    return Result<Guid>.Failure(Error.NotFound("Hizmet", request.ServiceId.ToString()));

                // 4. Hizmet isletmeye ait mi?
                if (service.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.BusinessRule("Hizmet bu isletmeye ait degil."));

                // 5. Ayni isimde baska hizmet var mi?
                var existingServices = await _serviceRepository.FindAsync(
                    s => s.Name == request.Name && s.BusinessId == request.BusinessId && s.Id != request.ServiceId && !s.IsDeleted,
                    cancellationToken);

                if (existingServices.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu isimde bir hizmet zaten mevcut."));

                // 6. Domain kurallari ile guncelle
                var money = new Money(request.Price, request.Currency);
                service.UpdateService(
                    request.Name,
                    request.Description,
                    money,
                    request.DurationInMinutes,
                    request.Category);

                // 7. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(service.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Hizmet guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}