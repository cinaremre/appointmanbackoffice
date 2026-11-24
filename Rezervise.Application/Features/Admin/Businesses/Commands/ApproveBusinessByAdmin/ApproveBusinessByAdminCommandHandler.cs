using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.ApproveBusinessByAdmin
{
    /// <summary>
    /// İşletme onaylama handler'i. Onay, Business.Approve metoduyla entity üzerinde
    /// yapılır; "Approve" aksiyonu denetim kaydına yazılır ve işletme sahibine
    /// "hoşgeldiniz" bildirimi oluşturulur.
    /// </summary>
    public class ApproveBusinessByAdminCommandHandler : IRequestHandler<ApproveBusinessByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public ApproveBusinessByAdminCommandHandler(
            IRepository<Business> businessRepository,
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(ApproveBusinessByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Yönetici kimliği denetim kaydı için zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 2. İşletme kontrolü.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 3. Zaten onaylı işletme tekrar onaylanamaz.
                if (business.IsApproved)
                    return Result<Guid>.Failure(Error.Conflict("İşletme zaten onaylı."));

                // 4. Önceki durum denetim kaydı için yakalanır.
                var oldValues = JsonSerializer.Serialize(new { IsApproved = false, Status = "PendingApproval" });

                // 5. Entity'nin kendi onay akışı kullanılır.
                business.Approve();

                // 6. Sahibine onay bildirimi: mobil uygulamanın bildirim kutusuna düşer.
                var notification = new Notification(
                    business.OwnerId,
                    "İşletmeniz Onaylandı",
                    "İşletmeniz onaylanmıştır, Rezervise dünyasına hoşgeldiniz!",
                    EnumNotificationType.BusinessVerification,
                    $"/businesses/{business.Id}");

                await _notificationRepository.AddAsync(notification, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 7. Denetim kaydı: Action=Approve, önceki durum bilgisiyle.
                await _auditSink.RecordAsync(
                    _currentUser.UserId.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "Approve",
                    oldValues,
                    JsonSerializer.Serialize(new { IsApproved = true, Status = "Active" }),
                    string.IsNullOrWhiteSpace(request.Reason) ? "Backoffice onayı" : request.Reason,
                    _auditContext.CorrelationId,
                    _auditContext.IpAddress,
                    cancellationToken);

                return Result<Guid>.Success(business.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("İşletme onaylanırken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydına yazılacak rol: bilinen ilk rol; roller çözülemiyorsa
        /// geriye dönük uyumluluk için "Admin" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "Admin";
    }
}
