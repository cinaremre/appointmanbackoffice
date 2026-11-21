using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory
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
                // Kritik islem: gerekce zorunlu (dokuman Modul 9).
                if (string.IsNullOrWhiteSpace(request.Reason))
                    return Result<Guid>.Failure(
                        Error.Validation("Arsivleme gerekcesi (reason) zorunludur."));

                var category = await _categoryRepository.GetByIdAsync(
                    request.CategoryId, cancellationToken);

                if (category is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                // Aktif cocuk varsa arsivleme engellenir; once cocuklar arsivlenir.
                var activeChildren = await _categoryRepository.FindAsync(
                    c => c.ParentCategoryId == request.CategoryId && c.IsActive && !c.IsDeleted,
                    cancellationToken);

                if (activeChildren.Any())
                    return Result<Guid>.Failure(
                        Error.Conflict("Kategorinin aktif alt kategorileri var; once onlari arsivleyin."));

                // Arsiv = pasif mekanizma; entity'de yeni metot eklenmez.
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
                    Error.Internal("Kategori arsivlenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
