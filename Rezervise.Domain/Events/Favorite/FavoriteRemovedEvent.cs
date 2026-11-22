namespace Rezervise.Domain.Events.Favorite
{
    public class FavoriteRemovedEvent : FavoriteEventBase
    {
        public string? RemovedBy { get; }

        public FavoriteRemovedEvent(
            Guid favoriteId,
            Guid userId,
            Guid businessId,
            string? removedBy = null) : base(favoriteId, userId, businessId)
        {
            RemovedBy = removedBy;
        }
    }
}