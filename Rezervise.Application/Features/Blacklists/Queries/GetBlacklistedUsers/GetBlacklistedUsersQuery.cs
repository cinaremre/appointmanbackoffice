using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Queries.GetBlacklistedUsers
{
    public class GetBlacklistedUsersQuery : IRequest<Result<List<BlacklistEntryDto>>>
    {
        public Guid BusinessId { get; set; }
        public Guid RequesterId { get; set; }
        public bool IncludeRemoved { get; set; } = false;  // Cikarilanlari da goster?
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}