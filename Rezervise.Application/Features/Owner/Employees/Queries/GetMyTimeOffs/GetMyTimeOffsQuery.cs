using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Queries.GetMyTimeOffs
{
    /// <summary>
    /// İşletme sahibinin kendi çalışanlarının izinleri. EmployeeTimeOff
    /// entity'sinde BusinessId alani bulunmadığından kapsam, işletmenin
    /// çalışanları üzerinden kurulur.
    /// </summary>
    public class GetMyTimeOffsQuery : IRequest<Result<AdminPagedResult<OwnerTimeOffListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Durum filtresi; boş ise tüm izinler.</summary>
        public OwnerTimeOffStatus? Status { get; set; }

        /// <summary>1 tabanlı sayfa numarası.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sınırlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Owner izin durum sayısal değerleri. Entity'de ayrı durum alani
    /// yoktur: onay bayrağı ve soft-delete üzerinden türetilir
    /// (Reject = soft delete).
    /// </summary>
    public enum OwnerTimeOffStatus
    {
        /// <summary>Henüz karar verilmemiş izin.</summary>
        Pending = 0,

        /// <summary>Sahip tarafından onaylanmış izin.</summary>
        Approved = 1,

        /// <summary>Reddedilmiş izin (soft-delete kaydı).</summary>
        Rejected = 2
    }

    /// <summary>Owner izin liste satırı.</summary>
    public class OwnerTimeOffListItemDto
    {
        public Guid Id { get; init; }

        public Guid EmployeeId { get; init; }

        public DateTime StartDate { get; init; }

        public DateTime EndDate { get; init; }

        /// <summary>Durum sayısal değer olarak döner (OwnerTimeOffStatus).</summary>
        public OwnerTimeOffStatus Status { get; init; }

        public string? Reason { get; init; }
    }
}
