using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.MoveBusinessCategory
{
    public class MoveBusinessCategoryCommandHandler
        : IRequestHandler<MoveBusinessCategoryCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public MoveBusinessCategoryCommandHandler(
            IRepository<BusinessCategory> categoryRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IAdminAuditSink auditSink,
            IAdminAuditContext auditContext)
        {
            _categoryRepository = categoryRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _auditSink = auditSink;
            _auditContext = auditContext;
        }

        public async Task<Result<Guid>> Handle(
            MoveBusinessCategoryCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Döngü koruması için tüm ağaç tek seferde çekilir; üst zinciri
                // ParentId ilişkileri üzerinden yürüme navigation'a bağlı değildir.
                var categories = await _categoryRepository.FindAsync(
                    c => !c.IsDeleted,
                    cancellationToken);

                var byId = categories.ToDictionary(c => c.Id);

                if (!byId.TryGetValue(request.CategoryId, out var category))
                    return Result<Guid>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // Kök seviyesine taşıma: döngü riski yok.
                if (request.TargetParentId is null)
                {
                    var oldParentId = category.ParentCategoryId;
                    category.SetParentCategory(null);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    await AdminAuditRecorder.RecordAsync(
                        _auditSink,
                        _auditContext,
                        _currentUser.UserId,
                        businessId: null,
                        entityType: "BusinessCategory",
                        entityId: category.Id.ToString(),
                        action: "Move",
                        oldValues: $"{{\"parentId\":\"{oldParentId?.ToString() ?? "null"}\"}}",
                        newValues: "{\"parentId\":\"null\"}",
                        cancellationToken: cancellationToken);

                    return Result<Guid>.Success(category.Id);
                }

                if (request.TargetParentId.Value == request.CategoryId)
                    return Result<Guid>.Failure(
                        Error.Conflict("Kategori kendisinin alt kategorisi olamaz."));

                if (!byId.TryGetValue(request.TargetParentId.Value, out var targetParent))
                    return Result<Guid>.Failure(
                        Error.NotFound("Üst kategori", request.TargetParentId.Value.ToString()));

                // 9.3 Döngü koruması: hedef üstün zincirinde taşınan kategori
                // kendisi varsa taşıma döngü oluşturur. Örnek: A, B'nin çocuğu ise
                // B, A'nin altına taşınamaz.
                var cursor = targetParent;
                var guard = 0;
                while (cursor is not null)
                {
                    if (cursor.Id == request.CategoryId)
                        return Result<Guid>.Failure(
                            Error.Conflict("Kategori ağacında döngü oluşmaz."));

                    cursor = cursor.ParentCategoryId.HasValue
                        && byId.TryGetValue(cursor.ParentCategoryId.Value, out var ancestor)
                        ? ancestor
                        : null;

                    // Bozuk veri koruması: derinlik toplam kayıt sayısını aşamaz.
                    guard++;
                    if (guard > categories.Count + 1)
                        return Result<Guid>.Failure(
                            Error.Internal("Kategori zinciri bozuk; döngü taraması başarısız."));
                }

                var oldParent = category.ParentCategoryId;
                category.SetParentCategory(targetParent);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    businessId: null,
                    entityType: "BusinessCategory",
                    entityId: category.Id.ToString(),
                    action: "Move",
                    oldValues: $"{{\"parentId\":\"{oldParent?.ToString() ?? "null"}\"}}",
                    newValues: $"{{\"parentId\":\"{targetParent.Id}\"}}",
                    cancellationToken: cancellationToken);

                return Result<Guid>.Success(category.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(
                    Error.Internal("Kategori taşınırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
