using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin
{
    public class CreateBusinessCategoryByAdminCommandHandler
        : IRequestHandler<CreateBusinessCategoryByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public CreateBusinessCategoryByAdminCommandHandler(
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
            CreateBusinessCategoryByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<Guid>.Failure(Error.Validation("Kategori adi zorunludur."));

                var name = request.Name.Trim();

                // 1. Ust kategori var mi?
                BusinessCategory? parent = null;
                if (request.ParentId.HasValue)
                {
                    parent = await _categoryRepository.GetByIdAsync(
                        request.ParentId.Value, cancellationToken);

                    if (parent is null)
                        return Result<Guid>.Failure(
                            Error.NotFound("Ust kategori", request.ParentId.Value.ToString()));
                }

                // 2. Ayni ada sahip aktif kardes karari (ilk surum kurali).
                var sameName = await _categoryRepository.FindAsync(
                    c => c.Name == name
                        && c.ParentCategoryId == request.ParentId
                        && c.IsActive
                        && !c.IsDeleted,
                    cancellationToken);

                if (sameName.Any())
                    return Result<Guid>.Failure(
                        Error.Conflict("Ayni ada sahip aktif bir kardes kategori zaten mevcut."));

                // 3. Domain entity olustur ve ust iliskisini kur.
                var category = new BusinessCategory(name, request.DisplayOrder);

                if (parent is not null)
                    category.SetParentCategory(parent);

                await _categoryRepository.AddAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 4. Denetim kaydi: kritik yonetici islemi.
                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    businessId: null,
                    entityType: "BusinessCategory",
                    entityId: category.Id.ToString(),
                    action: "Create",
                    newValues: $"{{\"name\":\"{category.Name}\",\"parentId\":\"{request.ParentId?.ToString() ?? "null"}\",\"displayOrder\":{category.DisplayOrder}}}",
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
                    Error.Internal("Kategori olusturulurken bir hata olustu.", ex.Message));
            }
        }
    }
}
