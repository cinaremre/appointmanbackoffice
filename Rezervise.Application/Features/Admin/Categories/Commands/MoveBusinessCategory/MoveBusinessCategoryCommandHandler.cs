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
                // Dongu korumasi icin tum agac tek seferde cekilir; ust zinciri
                // ParentId iliskileri uzerinden yurume navigation'a bagli degildir.
                var categories = await _categoryRepository.FindAsync(
                    c => !c.IsDeleted,
                    cancellationToken);

                var byId = categories.ToDictionary(c => c.Id);

                if (!byId.TryGetValue(request.CategoryId, out var category))
                    return Result<Guid>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // Kok seviyesine tasima: dongu riski yok.
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
                        Error.NotFound("Ust kategori", request.TargetParentId.Value.ToString()));

                // 9.3 Dongu korumasi: hedef ustun zincirinde tasinan kategori
                // kendisi varsa tasima dongu olusturur. Ornek: A, B'nin cocugu ise
                // B, A'nin altina tasinamaz.
                var cursor = targetParent;
                var guard = 0;
                while (cursor is not null)
                {
                    if (cursor.Id == request.CategoryId)
                        return Result<Guid>.Failure(
                            Error.Conflict("Kategori agacinda dongu olusmaz."));

                    cursor = cursor.ParentCategoryId.HasValue
                        && byId.TryGetValue(cursor.ParentCategoryId.Value, out var ancestor)
                        ? ancestor
                        : null;

                    // Bozuk veri korumasi: derinlik toplam kayit sayisini asamaz.
                    guard++;
                    if (guard > categories.Count + 1)
                        return Result<Guid>.Failure(
                            Error.Internal("Kategori zinciri bozuk; dongu taramasi basarisiz."));
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
                    Error.Internal("Kategori tasinirken bir hata olustu.", ex.Message));
            }
        }
    }
}
