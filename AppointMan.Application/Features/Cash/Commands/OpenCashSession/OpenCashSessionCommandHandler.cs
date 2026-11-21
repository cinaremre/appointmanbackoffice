using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Cash.Commands.OpenCashSession
{
    public class OpenCashSessionCommandHandler : IRequestHandler<OpenCashSessionCommand, Result<Guid>>
    {
        private readonly IRepository<CashRegister> _cashRegisterRepository;
        private readonly IRepository<CashSession> _cashSessionRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeService _dateTimeService;

        public OpenCashSessionCommandHandler(
            IRepository<CashRegister> cashRegisterRepository,
            IRepository<CashSession> cashSessionRepository,
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IDateTimeService dateTimeService)
        {
            _cashRegisterRepository = cashRegisterRepository;
            _cashSessionRepository = cashSessionRepository;
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(OpenCashSessionCommand request, CancellationToken cancellationToken)
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
                    return Result<Guid>.Failure(Error.Forbidden("Bu kasa icin oturum acma yetkiniz yok."));

                // 3. Deaktif kasada oturum acilmaz
                if (!register.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Kasa deaktif oldugu icin oturum acilamaz."));

                // 4. Ayni kasada acik oturum var mi?
                var openSessions = await _cashSessionRepository.FindAsync(
                    s => s.CashRegisterId == register.Id &&
                         s.Status == EnumCashSessionStatus.Open &&
                         !s.IsDeleted,
                    cancellationToken);

                if (openSessions.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu kasada acik bir oturum zaten var."));

                // 5. Oturumu ac
                var session = new CashSession(
                    register.Id,
                    register.BusinessId,
                    _currentUser.UserId!.Value,
                    request.OpeningCash,
                    _dateTimeService.UtcNow);

                await _cashSessionRepository.AddAsync(session, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(session.Id);
            }
            catch (DuplicateKeyException)
            {
                // Unique filtreli index: (CashRegisterId, Status=Open).
                return Result<Guid>.Failure(Error.Conflict("Bu kasada acik bir oturum zaten var."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kasa oturumu acilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
