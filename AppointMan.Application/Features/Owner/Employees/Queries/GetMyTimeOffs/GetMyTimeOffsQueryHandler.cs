using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Employees.Queries.GetMyTimeOffs
{
    /// <summary>
    /// Isletme sahibinin izin listesi. EmployeeTimeOff entity'sinde
    /// BusinessId bulunmadigindan kapsam soyle kurulur: once isletmenin
    /// calisanlari yuklenir, sonra bu calisanlarin izinleri listelenir.
    /// Durum, onay bayragi ve soft-delete uzerinden turetilir.
    /// </summary>
    public class GetMyTimeOffsQueryHandler
        : IRequestHandler<GetMyTimeOffsQuery, Result<AdminPagedResult<OwnerTimeOffListItemDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<EmployeeTimeOff> _timeOffRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyTimeOffsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            IRepository<EmployeeTimeOff> timeOffRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _timeOffRepository = timeOffRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerTimeOffListItemDto>>> Handle(
            GetMyTimeOffsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                // 3. Kapsam: isletmenin silinmemis calisanlarinin ID'leri.
                //    EmployeeTimeOff BusinessId tasimadigindan bu join
                //    bellek icinde kurulur (ilk surum).
                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId && !e.IsDeleted,
                    cancellationToken);

                var employeeIds = employees.Select(e => e.Id).ToList();

                if (employeeIds.Count == 0)
                    return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Success(
                        new AdminPagedResult<OwnerTimeOffListItemDto>());

                // 4. Bu calisanlarin izinleri (reddedilenler dahil: soft-delete
                //    kayitlari "Rejected" durumunda listelenir).
                var timeOffs = await _timeOffRepository.FindAsync(
                    t => employeeIds.Contains(t.EmployeeId),
                    cancellationToken);

                // 5. Durum turetme ve opsiyonel durum filtresi.
                var projected = timeOffs
                    .Select(t => new { TimeOff = t, Status = ResolveStatus(t) })
                    .Where(x => request.Status == null || x.Status == request.Status)
                    .ToList();

                // 6. Kararli siralama: en yeni baslangic once.
                var ordered = projected
                    .OrderByDescending(x => x.TimeOff.StartDate)
                    .ThenBy(x => x.TimeOff.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new OwnerTimeOffListItemDto
                    {
                        Id = x.TimeOff.Id,
                        EmployeeId = x.TimeOff.EmployeeId,
                        StartDate = x.TimeOff.StartDate,
                        EndDate = x.TimeOff.EndDate,
                        Status = x.Status,
                        Reason = x.TimeOff.Reason
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Success(
                    new AdminPagedResult<OwnerTimeOffListItemDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Failure(
                    Error.Internal("Izin listesi getirilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>
        /// Durum turetme: soft-delete edilmis kayit Rejected, onayli kayit
        /// Approved, digerleri Pending'tir.
        /// </summary>
        private static OwnerTimeOffStatus ResolveStatus(EmployeeTimeOff timeOff)
        {
            if (timeOff.IsDeleted)
                return OwnerTimeOffStatus.Rejected;

            return timeOff.IsApproved
                ? OwnerTimeOffStatus.Approved
                : OwnerTimeOffStatus.Pending;
        }
    }
}
