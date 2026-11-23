using Rezervise.Application.Common.Results;
using MediatR;
using System.Text.Json.Serialization;

namespace Rezervise.Application.Features.Appointments.Commands.CreateAppointment
{
    public class CreateAppointmentCommand : IRequest<Result<Guid>>
    {
        // Kim oluşturuyor? (JWT'den alınacak)
        [JsonIgnore]
        public Guid RequesterId { get; set; }

        // Randevu bilgileri
        public Guid UserId { get; set; }
        public Guid BusinessId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? Notes { get; set; }

        /// <summary>
        /// Aynı isteğin ağ tekrarı veya çift tıklama ile mükerrer gönderilmesini
        /// engelleyen istemci anahtarı. Header'dan okunur, gövdede beklenmez.
        /// </summary>
        [JsonIgnore]
        public string? IdempotencyKey { get; set; }
    }
}