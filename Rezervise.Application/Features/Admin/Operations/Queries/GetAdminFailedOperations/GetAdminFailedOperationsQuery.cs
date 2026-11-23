using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Başarısız operasyonlar özeti (doküman Modül 20): Failed durumundaki
    /// aktarım işleri ve örnek niteliğinde başarısız satır kayıtları.
    /// </summary>
    public class GetAdminFailedOperationsQuery : IRequest<Result<AdminFailedOperationsDto>>
    {
    }
}
