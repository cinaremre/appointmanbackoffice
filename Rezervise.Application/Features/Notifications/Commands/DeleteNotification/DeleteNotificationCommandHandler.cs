using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Commands.DeleteNotification
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
                // 1. Bildirim kontrolü (silinmiş kayıtlar hariç)
                var notifications = await _notificationRepository.FindAsync(
                    n => n.Id == request.NotificationId && !n.IsDeleted,
                    cancellationToken);

                var notification = notifications.FirstOrDefault();

                if (notification is null)
                    return Result<Guid>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 2. Yetki kontrolü.
                //    Aktör kimliği imzalı token'dan gelir; istekteki alan kullanıcıyı temsil etmez.
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
                return Result<Guid>.Failure(Error.Internal("Bildirim silinirken bir hata oluştu.", ex.Message));
            }
        }
    }
}