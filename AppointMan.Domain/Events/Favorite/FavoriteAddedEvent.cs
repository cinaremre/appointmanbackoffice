namespace AppointMan.Domain.Events.Favorite
{
    public class FavoriteAddedEvent : FavoriteEventBase
    {
        public string? Note { get; }

        public FavoriteAddedEvent(
            Guid favoriteId,
            Guid userId,
            Guid businessId,
            string? note = null) : base(favoriteId, userId, businessId)
        {
            Note = note;
        }
    }
}