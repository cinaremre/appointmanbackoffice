using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Employees.Commands.ApproveTimeOff;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Commands.ApproveMyTimeOff
{
    /// <summary>
    /// İşletme sahibinin izin onayı. Bu handler kendi onay algorıtmasını
    /// yazmaz: sahiplik ve kapsam doğrulaması yapar (izin, sahibin
    /// işletmesinin çalışanına ait olmalı), ardından işlemi mevcut
    /// <see cref="ApproveTimeOffCommand"/> ile IMediator üzerinden gerçek
    /// handler'a delege eder. Randevu çakışma kuralları böylece tek
    /// kaynaktan yürütülür. Başarı sonrası denetim kaydı yazılır
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
                // 1. Sahip kimliği JWT'den çözülür.
                if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.Unauthorized"));

                var actorId = _currentUser.UserId.Value;
                var actorRole = _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";

                // 2. İşletme sahipliği: owner kapsamının tek kapısı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                // 3. İzin kaydı: EmployeeTimeOff BusinessId taşımadığından
                //    kapsam çalışan üzerinden doğrulanır.
                var timeOff = await _timeOffRepository.GetByIdAsync(
                    request.TimeOffId, cancellationToken);

                if (timeOff is null || timeOff.IsDeleted)
                    return Result<Guid>.Failure(Error.NotFoundById("İzin", request.TimeOffId));

                var employee = await _employeeRepository.GetByIdAsync(
                    timeOff.EmployeeId, cancellationToken);

                if (employee is null)
                    return Result<Guid>.Failure(Error.NotFoundById("Çalışan", timeOff.EmployeeId));

                if (employee.BusinessId != request.BusinessId)
                    return Result<Guid>.Failure(Error.Forbidden("Bu izin işletmenizin kapsamında değil."));

                var oldStatus = timeOff.IsApproved ? "Approved" : "Pending";

                // 4. Domain onayı: mevcut ApproveTimeOffCommand ile gerçek
                //    handler'a delege edilir. RequesterId JWT kimliğidir;
                //    çakışma ve yetki kontrolleri tek kaynaktan yürür.
                var approveResult = await _mediator.Send(
                    new ApproveTimeOffCommand
                    {
                        TimeOffId = request.TimeOffId,
                        RequesterId = actorId
                    },
                    cancellationToken);

                // 5. Domain onayı başarısızsa aynı hatayı döndür; denetim
                //    kaydı yazılmaz.
                if (approveResult.IsFailure)
                    return approveResult;

                // 6. Başarı sonrası denetim kaydı.
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
                    "İzin işletme sahibi onayı sırasında bir hata oluştu.", ex.Message));
            }
        }
    }
}
