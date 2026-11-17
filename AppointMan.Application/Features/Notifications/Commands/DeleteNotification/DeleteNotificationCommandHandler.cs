using AppointMan.Application.Common.Interfaces;
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
        private readonly ICurrentUserService _currentUser;

        public DeleteNotificationCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirim kontrolu (silinmis kayitlar haric)
                var notifications = await _notificationRepository.FindAsync(
                    n => n.Id == request.NotificationId && !n.IsDeleted,
                    cancellationToken);

                var notification = notifications.FirstOrDefault();

                if (notification is null)
                    return Result<Guid>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 2. Yetki kontrolu.
                //    Aktor kimligi imzali token'dan gelir; istekteki alan kullaniciyi temsil etmez.
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<Guid>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                if (notification.UserId != currentUserId.Value)
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