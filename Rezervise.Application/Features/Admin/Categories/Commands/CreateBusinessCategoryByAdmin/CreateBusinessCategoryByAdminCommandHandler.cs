using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin
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
                    return Result<Guid>.Failure(Error.Validation("Kategori adı zorunludur."));

                var name = request.Name.Trim();

                // 1. Üst kategori var mi?
                BusinessCategory? parent = null;
                if (request.ParentId.HasValue)
                {
                    parent = await _categoryRepository.GetByIdAsync(
                        request.ParentId.Value, cancellationToken);

                    if (parent is null)
                        return Result<Guid>.Failure(
                            Error.NotFound("Üst kategori", request.ParentId.Value.ToString()));
                }

                // 2. Aynı ada sahip aktif kardeş kararı (ilk sürüm kuralı).
                var sameName = await _categoryRepository.FindAsync(
                    c => c.Name == name
                        && c.ParentCategoryId == request.ParentId
                        && c.IsActive
                        && !c.IsDeleted,
                    cancellationToken);

                if (sameName.Any())
                    return Result<Guid>.Failure(
                        Error.Conflict("Aynı ada sahip aktif bir kardeş kategori zaten mevcut."));

                // 3. Domain entity oluştur ve üst ilişkisini kur.
                var category = new BusinessCategory(name, request.DisplayOrder);

                if (parent is not null)
                    category.SetParentCategory(parent);

                await _categoryRepository.AddAsync(category, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 4. Denetim kaydı: kritik yönetici işlemi.
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
                    Error.Internal("Kategori oluşturulurken bir hata oluştu.", ex.Message));
            }
        }
    }
}
