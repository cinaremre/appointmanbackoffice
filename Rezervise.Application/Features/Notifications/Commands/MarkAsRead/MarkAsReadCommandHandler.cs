using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result<Guid>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public MarkAsReadCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<Guid>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
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
                    return Result<Guid>.Failure(Error.Unauthorized("Bu bildirimi okuma yetkiniz yok."));

                // 3. Zaten okundu mü?
                if (notification.IsRead)
                    return Result<Guid>.Failure(Error.Conflict("Bildirim zaten okundu."));

                // 4. Okundu olarak işaretle
                notification.MarkAsRead();

                // 5. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<Guid>.Success(notification.Id);
            }
            catch (DomainException ex)
            {
                return Result<Guid>.Failure(Error.BusinessRule(ex.Message));
            }
            catch (Exception ex)
            {
                return Result<Guid>.Failure(Error.Internal("Bildirim okundu işaretlenirken bir hata oluştu.", ex.Message));
            }
        }
    }
}