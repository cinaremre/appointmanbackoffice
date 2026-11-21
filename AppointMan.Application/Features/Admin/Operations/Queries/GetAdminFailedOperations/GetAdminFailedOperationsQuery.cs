using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Basarisiz operasyonlar ozeti (dokuman Modul 20): Failed durumundaki
    /// aktarim isleri ve ornek niteliginde basarisiz satir kayitlari.
    /// </summary>
    public class GetAdminFailedOperationsQuery : IRequest<Result<AdminFailedOperationsDto>>
    {
    }
}
