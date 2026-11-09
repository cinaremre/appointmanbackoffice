using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Blacklists.Commands.AddToBlacklist
{
    public class AddToBlacklistCommand : IRequest<Result<Guid>>
    {
        // JWT'den alinacak (isletme sahibi)
        public Guid RequesterId { get; set; }

        // Kara listeye alinacak kullanici
        public Guid UserId { get; set; }

        // Hangi isletme
        public Guid BusinessId { get; set; }

        // Sebep (opsiyonel)
        public string? Reason { get; set; }
    }
}