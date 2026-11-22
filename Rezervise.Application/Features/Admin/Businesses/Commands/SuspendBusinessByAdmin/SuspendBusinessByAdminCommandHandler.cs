using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Commands.SuspendBusinessByAdmin
{
    /// <summary>
    /// Isletme askiya alma handler'i (dokuman Modul 8). Durum degisikligi
    /// Business.Suspend metoduyla yapilir; "Suspend" aksiyonu denetim
    /// kaydina onceki durum bilgisiyle yazilir.
    /// </summary>
    public class SuspendBusinessByAdminCommandHandler : IRequestHandler<SuspendBusinessByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public SuspendBusinessByAdminCommandHandler(
            IRepository<Business> businessRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _businessRepository = businessRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(SuspendBusinessByAdminCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Gerekce zorunludur; denetim kaydini anlamsiz kilmamak icin
                //    bos gonderim dogrulama hatasiyla reddedilir.
                if (string.IsNullOrWhiteSpace(request.Reason))
                    return Result<Guid>.Failure(
                        Error.Validation("Askida alma gerekcesi (Reason) zorunludur."));

                // 2. Yonetici kimligi denetim kaydi icin zorunludur.
                if (_currentUser.UserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized());

                // 3. Isletme kontrolu.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<Guid>.Failure(Error.NotFound("Isletme", request.BusinessId.ToString()));

                // 4. Zaten askida olan isletme tekrar askiya alinamaz.
                if (!business.IsActive)
                    return Result<Guid>.Failure(Error.Conflict("Isletme zaten askida."));

                // 5. Onceki durum denetim kaydi icin yakalanir.
                var oldValues = JsonSerializer.Serialize(new { Status = "Active" });

                // 6. Entity'nin kendi durum degisim akisi kullanilir.
                business.Suspend(_currentUser.UserId.Value.ToString(), request.Reason);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 7. Denetim kaydi: Action=Suspend, onceki durum bilgisiyle.
                await _auditSink.RecordAsync(
                    _currentUser.UserId.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "Suspend",
                    oldValues,
                    JsonSerializer.Serialize(new { Status = "Suspended" }),
                    request.Reason,
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
                    Error.Internal("Isletme askiya alinirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydina yazilacak rol: bilinen ilk rol; roller cozulemiyorsa
        /// geriye donuk uyumluluk icin "Admin" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "Admin";
    }
}
