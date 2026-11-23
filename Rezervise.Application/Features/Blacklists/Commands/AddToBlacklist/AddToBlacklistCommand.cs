using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommand : IRequest<Result<Guid>>
    {
        // JWT'den alınacak (işletme sahibi)
        public Guid RequesterId { get; set; }

        // Kara listeye alınacak kullanıcı
        public Guid UserId { get; set; }

        // Hangi işletme
        public Guid BusinessId { get; set; }

        // Sebep (opsiyonel)
        public string? Reason { get; set; }
    }
}