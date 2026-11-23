using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>
    /// Tek bir randevuyu kısa durum geçmişiyle getirir (doküman Modül 12).
    /// Geçmiş kayıtları CreatedAt/ChangedAt azalan sırada, en fazla 20 kayıt
    /// olarak döner. Randevu yoksa NotFound.
    /// </summary>
    public class GetAdminAppointmentDetailQuery : IRequest<Result<AdminAppointmentDetailDto>>
    {
        public Guid AppointmentId { get; set; }
    }
}
