using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Enums;
using MediatR;

namespace AppointMan.Application.Features.Services.Queries.SearchServices
{
    public class SearchServicesQuery : IRequest<Result<List<ServiceDto>>>
    {
        public Guid? BusinessId { get; set; }          // Opsiyonel: belirli isletmede ara
        public EnumServiceCategory? Category { get; set; }  // Kategori filtresi
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}