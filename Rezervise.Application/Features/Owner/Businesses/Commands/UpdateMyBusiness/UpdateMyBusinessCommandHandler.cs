using System.Text.Json;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Commands.UpdateMyBusiness
{
    /// <summary>
    /// İşletme sahibinin kendi işletme bilgilerini günceller. Alan
    /// değişikliği Business entity'nin Set metotlarıyla yapılır; başarı
    /// sonrası denetim kaydına "OwnerUpdateBusiness" aksiyonu yazılır.
    /// </summary>
    public class UpdateMyBusinessCommandHandler : IRequestHandler<UpdateMyBusinessCommand, Result<Guid>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateMyBusinessCommandHandler(
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

        public async Task<Result<Guid>> Handle(
            UpdateMyBusinessCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. En az bir alan gönderilmeli (PATCH şeması).
                var hasName = !string.IsNullOrWhiteSpace(request.Name);
                var hasDescription = request.Description is not null;

                if (!hasName && !hasDescription)
                    return Result<Guid>.Failure(
                        Error.Validation("Güncellenecek en az bir alan (Name veya Description) gereklidir."));

                // 2. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<Guid>.Failure(owned.Error!);

                var business = owned.Value!;

                // 3. Önceki değerler denetim kaydı için yakalanır.
                var oldValues = JsonSerializer.Serialize(new
                {
                    business.Name,
                    business.Description
                });

                // 4. Entity'nin kendi güncelleme metotları kullanılır.
                if (hasName)
                    business.SetName(request.Name!.Trim());

                if (hasDescription)
                    business.SetDescription(request.Description);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 5. Denetim kaydı: actor JWT kimliğinden çözülür.
                await _auditSink.RecordAsync(
                    _currentUser.UserId!.Value,
                    ResolveActorRole(),
                    business.Id,
                    "Business",
                    business.Id.ToString(),
                    "OwnerUpdateBusiness",
                    oldValues,
                    JsonSerializer.Serialize(new { business.Name, business.Description }),
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
                    Error.Internal("İşletme güncellenirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Denetim kaydına yazılacak rol: bilinen ilk rol; roller çözülemiyorsa
        /// geriye dönük uyumluluk için "BusinessOwner" sabiti.
        /// </summary>
        private string ResolveActorRole()
            => _currentUser.Roles.FirstOrDefault() ?? "BusinessOwner";
    }
}
