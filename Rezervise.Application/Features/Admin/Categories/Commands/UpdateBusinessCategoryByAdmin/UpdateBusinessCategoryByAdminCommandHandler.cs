using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Categories.Commands.UpdateBusinessCategoryByAdmin
{
    public class UpdateBusinessCategoryByAdminCommandHandler
        : IRequestHandler<UpdateBusinessCategoryByAdminCommand, Result<Guid>>
    {
        private readonly IRepository<BusinessCategory> _categoryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IAdminAuditSink _auditSink;
        private readonly IAdminAuditContext _auditContext;

        public UpdateBusinessCategoryByAdminCommandHandler(
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
            UpdateBusinessCategoryByAdminCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request.Name is null && request.DisplayOrder is null && request.IsActive is null)
                    return Result<Guid>.Failure(
                        Error.Validation("Guncellenecek en az bir alan gereklidir."));

                var category = await _categoryRepository.GetByIdAsync(
                    request.CategoryId, cancellationToken);

                if (category is null)
                    return Result<Guid>.Failure(
                        Error.NotFound("Kategori", request.CategoryId.ToString()));

                var oldValues = $"{{\"name\":\"{category.Name}\",\"displayOrder\":{category.DisplayOrder},\"isActive\":{category.IsActive}}}";

                if (request.Name is not null)
                    category.SetName(request.Name.Trim());

                if (request.DisplayOrder.HasValue)
                    category.SetDisplayOrder(request.DisplayOrder.Value);

                if (request.IsActive.HasValue)
                {
                    if (request.IsActive.Value)
                        category.Activate();
                    else
                        category.Deactivate();
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var newValues = $"{{\"name\":\"{category.Name}\",\"displayOrder\":{category.DisplayOrder},\"isActive\":{category.IsActive}}}";

                await AdminAuditRecorder.RecordAsync(
                    _auditSink,
                    _auditContext,
                    _currentUser.UserId,
                    businessId: null,
                    entityType: "BusinessCategory",
                    entityId: category.Id.ToString(),
                    action: "Update",
                    oldValues,
                    newValues,
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
                    Error.Internal("Kategori guncellenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
