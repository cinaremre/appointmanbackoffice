using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Result<Guid>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkAsReadCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Bildirim kontrolu
                var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
                if (notification is null)
                    return Result<Guid>.Failure(Error.NotFound("Bildirim", request.NotificationId.ToString()));

                // 2. Yetki kontrolu
                if (notification.UserId != request.UserId)
                    return Result<Guid>.Failure(Error.Unauthorized("Bu bildirimi okuma yetkiniz yok."));

                // 3. Zaten okundu mu?
                if (notification.IsRead)
                    return Result<Guid>.Failure(Error.Conflict("Bildirim zaten okundu."));

                // 4. Okundu olarak isaretle
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
                return Result<Guid>.Failure(Error.Internal("Bildirim okundu isaretlenirken bir hata olustu.", ex.Message));
            }
        }
    }
}