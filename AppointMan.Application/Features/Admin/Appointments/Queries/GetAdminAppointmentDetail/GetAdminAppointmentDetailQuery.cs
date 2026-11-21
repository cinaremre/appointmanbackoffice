using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail
{
    /// <summary>
    /// Tek bir randevuyu kisa durum gecmisiyle getirir (dokuman Modul 12).
    /// Gecmis kayitlari CreatedAt/ChangedAt azalan sirada, en fazla 20 kayit
    /// olarak doner. Randevu yoksa NotFound.
    /// </summary>
    public class GetAdminAppointmentDetailQuery : IRequest<Result<AdminAppointmentDetailDto>>
    {
        public Guid AppointmentId { get; set; }
    }
}
