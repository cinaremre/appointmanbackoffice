using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Commands.RemoveFromBlacklist
{
    public class RemoveFromBlacklistCommand : IRequest<Result<Guid>>
    {
        public Guid BlacklistEntryId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid RequesterId { get; set; }
        public string? Reason { get; set; }
    }
}