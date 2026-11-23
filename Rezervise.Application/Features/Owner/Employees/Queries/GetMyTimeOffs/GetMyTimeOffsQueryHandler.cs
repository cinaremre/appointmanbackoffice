using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Common;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Employees.Queries.GetMyTimeOffs
{
    /// <summary>
    /// İşletme sahibinin izin listesi. EmployeeTimeOff entity'sinde
    /// BusinessId bulunmadığından kapsam söyle kurulur: önce işletmenin
    /// çalışanları yüklenir, sonra bu çalışanların izinleri listelenir.
    /// Durum, onay bayrağı ve soft-delete üzerinden türetilir.
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
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Failure(owned.Error!);

                // 2. Sayfalama kuralları.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                // 3. Kapsam: işletmenin silinmemiş çalışanlarının ID'leri.
                //    EmployeeTimeOff BusinessId taşımadığından bu join
                //    bellek içinde kurulur (ilk sürüm).
                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == request.BusinessId && !e.IsDeleted,
                    cancellationToken);

                var employeeIds = employees.Select(e => e.Id).ToList();

                if (employeeIds.Count == 0)
                    return Result<AdminPagedResult<OwnerTimeOffListItemDto>>.Success(
                        new AdminPagedResult<OwnerTimeOffListItemDto>());

                // 4. Bu çalışanların izinleri (reddedilenler dahil: soft-delete
                //    kayıtları "Rejected" durumunda listelenir).
                var timeOffs = await _timeOffRepository.FindAsync(
                    t => employeeIds.Contains(t.EmployeeId),
                    cancellationToken);

                // 5. Durum türetme ve opsiyonel durum filtresi.
                var projected = timeOffs
                    .Select(t => new { TimeOff = t, Status = ResolveStatus(t) })
                    .Where(x => request.Status == null || x.Status == request.Status)
                    .ToList();

                // 6. Kararlı sıralama: en yeni başlangıç önce.
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
                    Error.Internal("İzin listesi getirilirken bir hata oluştu.", ex.Message));
            }
        }

        /// <summary>
        /// Durum türetme: soft-delete edilmiş kayıt Rejected, onaylı kayıt
        /// Approved, diğerleri Pending'tir.
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
