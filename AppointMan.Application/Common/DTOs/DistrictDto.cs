namespace AppointMan.Application.Common.DTOs
{
    public class DistrictDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CityId { get; set; }
        public bool IsActive { get; set; }
    }
}
