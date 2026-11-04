using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.DeleteNotification
{
    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, Result<Guid>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNotificationCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirim kontrolu
                var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
                if (notification is null)
                    return Result<Guid>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 2. Yetki kontrolu
                if (notification.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu bildirimi silme yetkiniz yok."));

                // 3. Sil
                notification.Delete();

                // 4. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(notification.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Bildirim silinirken bir hata olustu.", ex.Message));
            }
        }
    }
}