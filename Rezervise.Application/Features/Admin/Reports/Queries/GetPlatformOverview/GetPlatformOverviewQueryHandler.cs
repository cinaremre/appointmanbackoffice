using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Reports.Queries.GetPlatformOverview
{
    /// <summary>
    /// Platform ozet handler'i (dokuman Modul 18). Ilk surum sayimlari
    /// repository FindAsync ile bellek icinde yapar; buyuk olcekte hazir
    /// ozet tablolarindan okunacaktir.
    /// </summary>
    public class GetPlatformOverviewQueryHandler : IRequestHandler<GetPlatformOverviewQuery, Result<PlatformOverviewDto>>
    {
        private readonly IRepository<Business> _businessRepository;
        private readonly IRepository<Appointment> _appointmentRepository;

        public GetPlatformOverviewQueryHandler(
            IRepository<Business> businessRepository,
            IRepository<Appointment> appointmentRepository)
        {
            _businessRepository = businessRepository;
            _appointmentRepository = appointmentRepository;
        }

        public async Task<Result<PlatformOverviewDto>> Handle(GetPlatformOverviewQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletme sayimlari: durumlar IsActive bayragi uzerinden.
                var businesses = await _businessRepository.FindAsync(
                    b => !b.IsDeleted, cancellationToken);

                var registrationCutoff = DateTime.UtcNow.AddDays(-30);

                var activeBusinesses = businesses.Count(b => b.IsActive);
                var suspendedBusinesses = businesses.Count(b => !b.IsActive);
                var newRegistrations = businesses.Count(b => b.CreatedAt >= registrationCutoff);

                // 2. Randevu sayimlari ve oranlar.
                var appointments = await _appointmentRepository.FindAsync(
                    a => !a.IsDeleted, cancellationToken);

                var totalAppointments = appointments.Count;
                var completedAppointments = appointments.Count(a => a.Status == EnumAppointmentStatus.Completed);
                var cancelledAppointments = appointments.Count(a => a.Status == EnumAppointmentStatus.Cancelled);
                var noShowAppointments = appointments.Count(a => a.Status == EnumAppointmentStatus.NoShow);

                // 3. Oranlar: sifir randevu durumunda sifira bolme engellenir.
                var cancellationRate = totalAppointments == 0
                    ? 0m
                    : Math.Round(cancelledAppointments * 100.0m / totalAppointments, 2);

                var noShowRate = totalAppointments == 0
                    ? 0m
                    : Math.Round(noShowAppointments * 100.0m / totalAppointments, 2);

                return Result<PlatformOverviewDto>.Success(new PlatformOverviewDto
                {
                    TotalBusinesses = businesses.Count,
                    ActiveBusinesses = activeBusinesses,
                    SuspendedBusinesses = suspendedBusinesses,
                    NewRegistrations30Days = newRegistrations,
                    TotalAppointments = totalAppointments,
                    CompletedAppointments = completedAppointments,
                    CancellationRate = cancellationRate,
                    NoShowRate = noShowRate
                });
            }
            catch (Exception ex)
            {
                return Result<PlatformOverviewDto>.Failure(
                    Error.Internal("Platform ozeti hesaplanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
