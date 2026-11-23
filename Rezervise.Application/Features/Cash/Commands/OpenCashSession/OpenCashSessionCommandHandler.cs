using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Cash.Commands.OpenCashSession
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
                // 1. Kasa kontrolü
                var register = await _cashRegisterRepository.GetByIdAsync(request.CashRegisterId, cancellationToken);
                if (register is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Kasa", request.CashRegisterId));

                // 2. Yetki kontrolü: kasanın işletmesi üzerinden business.OwnerId esas alınır.
                var business = await _businessRepository.GetByIdAsync(register.BusinessId, cancellationToken);
                if (business is null)
                    return Result<Guid>.Failure(Error.NotFoundById("İşletme", register.BusinessId));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<Guid>.Failure(Error.Forbidden("Bu kasa için oturum açma yetkiniz yok."));

                // 3. Deaktif kasada oturum açılmaz
                if (!register.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Kasa deaktif öldüğü için oturum açılamaz."));

                // 4. Aynı kasada açık oturum var mi?
                var openSessions = await _cashSessionRepository.FindAsync(
                    s => s.CashRegisterId == register.Id &&
                         s.Status == EnumCashSessionStatus.Open &&
                         !s.IsDeleted,
                    cancellationToken);

                if (openSessions.Any())
                    return Result<Guid>.Failure(Error.Conflict("Bu kasada açık bir oturum zaten var."));

                // 5. Oturumu aç
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
                return Result<Guid>.Failure(Error.Conflict("Bu kasada açık bir oturum zaten var."));
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Kasa oturumu açılırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
