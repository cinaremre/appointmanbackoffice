using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Owner.Common;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Owner.Businesses.Queries.GetMyBusinessDetail
{
    /// <summary>
    /// İşletme sahibinin kendi işletme detayı. Sahiplik OwnerScope ile
    /// doğrulanır; sayılar ayrı repository sorgularıyla hesaplanır
    /// (GetBusinessDetail deseniyle aynı).
    /// </summary>
    public class GetMyBusinessDetailQueryHandler
        : IRequestHandler<GetMyBusinessDetailQuery, Result<OwnerBusinessDetailDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Appointment> _appointmentRepository;
        private readonly ICurrentUserService _currentUser;

        public GetMyBusinessDetailQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Service> serviceRepository,
            IRepository<Appointment> appointmentRepository,
            ICurrentUserService currentUser)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<OwnerBusinessDetailDto>> Handle(
            GetMyBusinessDetailQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Sahiplik: işletme JWT sahibine ait olmalı.
                var owned = await OwnerScope.FindOwnedBusinessAsync(
                    _businessRepository, _currentUser, request.BusinessId, cancellationToken);

                if (owned.IsFailure)
                    return Result<OwnerBusinessDetailDto>.Failure(owned.Error!);

                var business = owned.Value!;
                var businessId = business.Id;

                // 2. Operasyonel sayılar: çalışan, aktif hizmet, bugünün randevuları.
                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == businessId && !e.IsDeleted,
                    cancellationToken);

                var services = await _serviceRepository.FindAsync(
                    s => s.BusinessId == businessId && s.IsActive && !s.IsDeleted,
                    cancellationToken);

                var dayStart = DateTime.UtcNow.Date;
                var dayEnd = dayStart.AddDays(1);

                var appointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == businessId
                        && a.AppointmentDate >= dayStart
                        && a.AppointmentDate < dayEnd
                        && !a.IsDeleted,
                    cancellationToken);

                // 3. Detay DTO'su.
                var dto = new OwnerBusinessDetailDto
                {
                    Id = business.Id,
                    Name = business.Name,
                    Description = business.Description,
                    IsActive = business.IsActive,
                    IsVerified = business.IsVerified,
                    Rating = business.Rating,
                    TotalReviews = business.TotalReviews,
                    CityId = business.Address?.CityId ?? 0,
                    Address = business.Address?.GetFullAddress() ?? string.Empty,
                    PhoneNumber = business.PhoneNumber?.Value ?? string.Empty,
                    Email = business.Email?.Value ?? string.Empty,
                    CreatedAt = business.CreatedAt,
                    ActiveServiceCount = services.Count,
                    EmployeeCount = employees.Count,
                    TodayAppointmentCount = appointments.Count
                };

                return Result<OwnerBusinessDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<OwnerBusinessDetailDto>.Failure(
                    Error.Internal("İşletme detayı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
