using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Common;
using AppointMan.Application.Features.Owner.Common;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Owner.Appointments.Queries.GetMyAppointments
{
    /// <summary>
    /// Isletme sahibinin randevu listesi. Sahiplik OwnerScope ile
    /// dogrulanir; randevular yalnizca istenen isletmenin kayitlarindan
    /// gelir.
    /// </summary>
    public class GetMyAppointmentsQueryHandler
        : IRequestHandler<GetMyAppointmentsQuery, Result<AdminPagedResult<OwnerAppointmentListItemDto>>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyAppointmentsQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<AdminPagedResult<OwnerAppointmentListItemDto>>> Handle(
            GetMyAppointmentsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: isletme JWT sahibine ait olmali.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<AdminPagedResult<OwnerAppointmentListItemDto>>.Failure(owned.Error!);

                // 2. Sayfalama kurallari backoffice ortak sinirlarindan gelir.
                var page = AdminPaging.NormalizePage(request.Page);
                var pageSize = AdminPaging.ClampPageSize(request.PageSize);

                var businessId = request.BusinessId;
                var status = request.Status;
                var fromDate = request.FromDate;
                var toDate = request.ToDate;

                // 3. Filtre: isletme + durum + tarih araligi (dahil).
                var appointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == businessId
                        && !a.IsDeleted
                        && (status == null || a.Status == status)
                        && (fromDate == null || a.AppointmentDate >= fromDate)
                        && (toDate == null || a.AppointmentDate <= toDate),
                    cancellationToken);

                // 4. En yeni randevu once; karisikligi onlemek icin Id sirasi.
                var ordered = appointments
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.Id)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new OwnerAppointmentListItemDto
                    {
                        Id = a.Id,
                        UserId = a.UserId,
                        EmployeeId = a.EmployeeId,
                        ServiceId = a.ServiceId,
                        AppointmentDate = a.AppointmentDate,
                        Status = a.Status,
                        Notes = a.Notes
                    })
                    .ToList();

                return Result<AdminPagedResult<OwnerAppointmentListItemDto>>.Success(
                    new AdminPagedResult<OwnerAppointmentListItemDto>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = ordered.Count
                    });
            }
            catch (Exception ex)
            {
                return Result<AdminPagedResult<OwnerAppointmentListItemDto>>.Failure(
                    Error.Internal("Randevular listelenirken bir hata olustu.", ex.Message));
            }
        }
    }
}
