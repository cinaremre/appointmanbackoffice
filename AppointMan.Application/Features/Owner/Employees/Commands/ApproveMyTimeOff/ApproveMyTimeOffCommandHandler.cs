using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Employees.Commands.ApproveTimeOff;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Employees.Commands.ApproveMyTimeOff
{
    /// <summary>
    /// Isletme sahibinin izin onayi. Bu handler kendi onay algoritmasini
    /// yazmaz: sahiplik ve kapsam dogrulamasi yapar (izin, sahibin
    /// isletmesinin calisanina ait olmali), ardindan islemi mevcut
    /// <see cref="ApproveTimeOffCommand"/> ile IMediator uzerinden gercek
    /// handler'a delege eder. Randevu cakisma kurallari boylece tek
    /// kaynaktan yurutulur. Basari sonrasi denetim kaydi yazilir
    /// (Action=OwnerApproveTimeOff).
    /// </summary>
    public class ApproveMyTimeOffCommandHandler : IRequestHandler<ApproveMyTimeOffCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<EmployeeTimeOff> _timeOffRepository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public ApproveMyTimeOffCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            IRepository<EmployeeTimeOff> timeOffRepository,
            IMediator mediator,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _timeOffRepository = timeOffRepository;
            _mediator = mediator;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(
            ApproveMyTimeOffCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahip kimligi JWT'den cozulur.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";

                // 2. Isletme sahipligi: owner kapsaminin tek kapisi.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                // 3. Izin kaydi: EmployeeTimeOff BusinessId tasimadigindan
                //    kapsam calisan uzerinden dogrulanir.
                var timeOff = await _timeOffRepository.GetByIdAsync(
                    request.TimeOffId, cancellationToken);

                if (timeOff is null || timeOff.IsDeleted)
                    return Result<Guid>.Failure(Error.NotFoundById("Izin", request.TimeOffId));

                var employee = await _employeeRepository.GetByIdAsync(
                    timeOff.EmployeeId, cancellationToken);

                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Calisan", timeOff.EmployeeId));

                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Forbidden("Bu izin isletmenizin kapsaminda degil."));

                var oldStatus = timeOff.IsApproved ? "Approved" : "Pending";

                // 4. Domain onayi: mevcut ApproveTimeOffCommand ile gercek
                //    handler'a delege edilir. RequesterId JWT kimligidir;
                //    cakisma ve yetki kontrolleri tek kaynaktan yurur.
                var approveResult = await _mediator.Send(
                    new ApproveTimeOffCommand
                    {
                        TimeOffId = request.TimeOffId,
                        RequesterId = actorId
                    },
                    cancellationToken);

                // 5. Domain onayi basarisizsa ayni hatayi dondur; denetim
                //    kaydi yazilmaz.
                if (approveResult.IsFailure)
                    return approveResult;

                // 6. Basari sonrasi denetim kaydi.
                await _auditSink.RecordAsync(
                    actorId,
                    actorRole,
                    request.BusinessId,
                    "EmployeeTimeOff",
                    timeOff.Id.ToString(),
                    "OwnerApproveTimeOff",
                    oldValues: $"{{\"status\":\"{oldStatus}\"}}",
                    newValues: $"{{\"status\":\"Approved\",\"employeeId\":\"{timeOff.EmployeeId}\"}}",
                    reason: request.Reason,
                    correlationId: _auditContext.CorrelationId,
                    ipAddress: _auditContext.IpAddress,
                    cancellationToken: cancellationToken);

                return approveResult;
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal(
                    "Izin isletme sahibi onayi sirasinda bir hata olustu.", ex.Message));
            }
        }
    }
}
