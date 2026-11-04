namespace AppointMan.Domain.Events.Favorite
{
    public abstract class FavoriteEventBase : DomainEventBase
    {
        public Guid FavoriteId { get; }
        public Guid UserId { get; }
        public Guid BusinessId { get; }

        protected FavoriteEventBase(
            Guid favoriteId,
            Guid userId,
            Guid businessId) : base()
        {
            FavoriteId = favoriteId;
            UserId = userId;
            BusinessId = businessId;
        }
    }
}