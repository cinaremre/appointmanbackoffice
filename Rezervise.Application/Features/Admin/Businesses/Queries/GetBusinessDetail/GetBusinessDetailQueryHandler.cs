using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Businesses.Queries.GetBusinessDetail
{
    /// <summary>
    /// İşletme detay handler'i (doküman Modül 8). Sayılar ayrı repository
    /// sorgularıyla hesaplanır; büyük ölçekte tek read model sorgusunda
    /// birleştirilecektir.
    /// </summary>
    public class GetBusinessDetailQueryHandler : IRequestHandler<GetBusinessDetailQuery, Result<AdminBusinessDetailDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Employee> _employeeRepository;
        private readonly IRepository<Service> _serviceRepository;
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetBusinessDetailQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Employee> employeeRepository,
            IRepository<Service> serviceRepository,
            IRepository<Appointment> appointmentRepository)
        {
            _businessRepository = businessRepository;
            _employeeRepository = employeeRepository;
            _serviceRepository = serviceRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<AdminBusinessDetailDto>> Handle(GetBusinessDetailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İşletme kaydı.
                var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);

                if (business is null)
                    return Result<AdminBusinessDetailDto>.Failure(
                        Error.NotFound("İşletme", request.BusinessId.ToString()));

                // 2. Operasyonel sayılar: çalışan, aktif hizmet, son 30 gün randevu.
                var businessId = business.Id;
                var cutoff = DateTime.UtcNow.AddDays(-30);

                var employees = await _employeeRepository.FindAsync(
                    e => e.BusinessId == businessId && !e.IsDeleted, cancellationToken);

                var services = await _serviceRepository.FindAsync(
                    s => s.BusinessId == businessId && s.IsActive && !s.IsDeleted, cancellationToken);

                var appointments = await _appointmentRepository.FindAsync(
                    a => a.BusinessId == businessId && a.AppointmentDate >= cutoff && !a.IsDeleted,
                    cancellationToken);

                // 3. Detay DTO'su.
                var dto = new AdminBusinessDetailDto
                {
                    Id = business.Id,
                    Name = business.Name,
                    Description = business.Description,
                    Status = business.IsActive ? "Active" : "Suspended",
                    CityId = business.Address?.CityId ?? 0,
                    Address = business.Address?.GetFullAddress() ?? string.Empty,
                    PhoneNumber = business.PhoneNumber?.Value ?? string.Empty,
                    Email = business.Email?.Value ?? string.Empty,
                    OwnerId = business.OwnerId,
                    IsVerified = business.IsVerified,
                    Rating = business.Rating,
                    TotalReviews = business.TotalReviews,
                    CreatedAt = business.CreatedAt,
                    EmployeeCount = employees.Count,
                    ActiveServiceCount = services.Count,
                    RecentAppointmentCount = appointments.Count
                };

                return Result<AdminBusinessDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<AdminBusinessDetailDto>.Failure(
                    Error.Internal("İşletme detayı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
