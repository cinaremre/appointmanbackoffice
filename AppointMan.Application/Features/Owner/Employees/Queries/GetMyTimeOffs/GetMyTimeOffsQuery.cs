using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Owner.Employees.Queries.GetMyTimeOffs
{
    /// <summary>
    /// Isletme sahibinin kendi calisanlarinin izinleri. EmployeeTimeOff
    /// entity'sinde BusinessId alani bulunmadigindan kapsam, isletmenin
    /// calisanlari uzerinden kurulur.
    /// </summary>
    public class GetMyTimeOffsQuery : IRequest<Result<AdminPagedResult<OwnerTimeOffListItemDto>>>
    {
        public Guid BusinessId { get; set; }

        /// <summary>Durum filtresi; bos ise tum izinler.</summary>
        public OwnerTimeOffStatus? Status { get; set; }

        /// <summary>1 tabanli sayfa numarasi.</summary>
        public int Page { get; set; } = 1;

        /// <summary>Sayfa boyutu; handler en fazla 200'e sinirlar.</summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Owner izin durum sayisal degerleri. Entity'de ayri durum alani
    /// yoktur: onay bayragi ve soft-delete uzerinden turetilir
    /// (Reject = soft delete).
    /// </summary>
    public enum OwnerTimeOffStatus
    {
        /// <summary>Henuz karar verilmemis izin.</summary>
        Pending = 0,

        /// <summary>Sahip tarafindan onaylanmis izin.</summary>
        Approved = 1,

        /// <summary>Reddedilmis izin (soft-delete kaydi).</summary>
        Rejected = 2
    }

    /// <summary>Owner izin liste satiri.</summary>
    public class OwnerTimeOffListItemDto
    {
        public Guid Id { get; init; }

        public Guid EmployeeId { get; init; }

        public DateTime StartDate { get; init; }

        public DateTime EndDate { get; init; }

        /// <summary>Durum sayisal deger olarak doner (OwnerTimeOffStatus).</summary>
        public OwnerTimeOffStatus Status { get; init; }

        public string? Reason { get; init; }
    }
}
