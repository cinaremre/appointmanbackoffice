namespace Rezervise.Application.Common.DTOs
{
    public class NeighborhoodDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Bağlı olduğu semt (locality). Semti olmayan mahalleler için null döner.
        /// </summary>
        public int? LocalityId { get; set; }

        public bool IsActive { get; set; }
    }
}
