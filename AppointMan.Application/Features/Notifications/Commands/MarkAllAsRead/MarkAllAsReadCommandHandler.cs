using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result<int>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkAllAsReadCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Okunmamis bildirimleri getir
                var notifications = await _notificationRepository.FindAsync(
                    n => n.UserId == request.UserId && !n.IsRead && !n.IsDeleted,
                    cancellationToken);

                var notificationList = notifications.ToList();

                if (!notificationList.Any())
                    return Result<int>.Success(0);

                // 2. Tumunu okundu olarak isaretle
                foreach (var notification in notificationList)
                {
                    notification.MarkAsRead();
                }

                // 3. Kaydet
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<int>.Success(notificationList.Count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure(Error.Internal("Bildirimler okundu isaretlenirken bir hata olustu.", ex.Message));
            }
        }
    }
}