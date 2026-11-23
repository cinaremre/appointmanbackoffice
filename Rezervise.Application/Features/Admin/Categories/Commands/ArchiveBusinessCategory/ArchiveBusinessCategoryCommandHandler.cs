using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory
{
    public class ArchiveBusinessCategoryCommandHandler
        : IRequestHandler<ArchiveBusinessCategoryCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public ArchiveBusinessCategoryCommandHandler(
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
            ArchiveBusinessCategoryCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Kritik işlem: gerekçe zorunlu (doküman Modül 9).
                if (string.IsNullOrWhiteSpace(request.Reason))
                    return Result<Guid>.Failure(
                        Error.Validation("Arşivleme gerekçesi (reason) zorunludur."));

                var category = await _categoryRepository.GetByIdAsync(
                    request.CategoryId, cancellationToken);

                if (category is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // Aktif çocuk varsa arşivleme engellenir; önce çocuklar arşivlenir.
                var activeChildren = await _categoryRepository.FindAsync(
                    c => c.ParentCategoryId == request.CategoryId && c.IsActive && !c.IsDeleted,
                    cancellationToken);

                if (activeChildren.Any())
                    return Result<Guid>.Failure(
                        Error.Conflict("Kategorinin aktif alt kategorileri var; önce onları arşivleyin."));

                // Arşiv = pasif mekanizma; entity'de yeni metot eklenmez.
                var wasActive = category.IsActive;
                category.Deactivate();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    businessId: null,
                    entityType: "BusinessCategory",
                    entityId: category.Id.ToString(),
                    action: "Archive",
                    oldValues: $"{{\"isActive\":{wasActive.ToString().ToLowerInvariant()}}}",
                    newValues: "{\"isActive\":false}",
                    reason: request.Reason,
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
                    Error.Internal("Kategori arşivlenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
