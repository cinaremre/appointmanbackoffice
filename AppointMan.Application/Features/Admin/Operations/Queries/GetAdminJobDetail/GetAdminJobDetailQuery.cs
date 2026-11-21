using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>
    /// Tek bir aktarim isinin detayi: is ozeti ve satir sonuclarinin ilk 200'i.
    /// </summary>
    public class GetAdminJobDetailQuery : IRequest<Result<AdminJobDetailDto>>
    {
        public Guid ImportJobId { get; set; }
    }
}
