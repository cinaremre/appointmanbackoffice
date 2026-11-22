using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Queries.GetMyEmployees
{
    /// <summary>
    /// Isletme sahibinin kendi calisan listesi. Sahiplik handler'da
    /// dogrulanir.
    /// </summary>
    public class GetMyEmployeesQuery : IRequest<Result<AdminPagedResult<OwnerEmployeeListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Owner calisan liste satiri.</summary>
    public class OwnerEmployeeListItemDto
    {
        public Guid Id { get; init; }

        public string FirstName { get; init; } = string.Empty;

        public string LastName { get; init; } = string.Empty;

        public string PhoneNumber { get; init; } = string.Empty;

        public bool IsActive { get; init; }
    }
}
