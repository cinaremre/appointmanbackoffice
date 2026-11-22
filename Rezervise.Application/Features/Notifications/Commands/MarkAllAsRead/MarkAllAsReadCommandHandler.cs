using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Result<int>>
    {
        private readonly IRepository<Notification> _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public MarkAllAsReadCommandHandler(
            IRepository<Notification> notificationRepository,
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<Result<int>> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 0. Kimlik her zaman imzali token'dan gelir; istekteki hicbir alan kullaniciyi temsil etmez.
                var currentUserId = _currentUser.UserId;
                if (currentUserId is null)
                    return Result<int>.Failure(Error.Unauthorized("Auth.InvalidUser"));

                // 1. Okunmamis bildirimleri getir
                var notifications = await _notificationRepository.FindAsync(
                    n => n.UserId == currentUserId.Value && !n.IsRead && !n.IsDeleted,
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