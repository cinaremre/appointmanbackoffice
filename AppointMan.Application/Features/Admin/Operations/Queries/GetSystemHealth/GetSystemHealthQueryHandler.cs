using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem saglik raporu (dokuman Modul 20).
    ///
    /// BASITLESTIRME (ilk surum): DbReachable gibi gercek baglanti testleri
    /// yerine uygulamanin ayakta oldugu bilgisi + aktarim is durum sayilari
    /// raporlanir. Saglik kontrolleri sonraki surumlerde Redis, mesaj brokeri
    /// ve bildirim saglayicisi gibi bilesimler icin genisletilecektir.
    /// </summary>
    public class GetSystemHealthQueryHandler
        : IRequestHandler<GetSystemHealthQuery, Result<SystemHealthDto>>
    {
        /// <summary>Saglik kapsami aciklamasi.</summary>
        private const string HealthNote =
            "Ilk surum saglik raporu: uygulamanin ayakta oldugu ve aktarim islerinin durum sayilari raporlanir. Veritabani, Redis, mesaj brokeri ve saglayici saglik kontrolleri sonraki surumlerde eklenecek.";

        private readonly IRepository<ImportJob> _jobRepository;

        public GetSystemHealthQueryHandler(IRepository<ImportJob> jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<Result<SystemHealthDto>> Handle(
            GetSystemHealthQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Sorgu dongusunu tamamliyorsa uygulama ve veri katmani ayaktadir.
                var jobs = await _jobRepository.FindAsync(
                    j => !j.IsDeleted,
                    cancellationToken);

                return Result<SystemHealthDto>.Success(new SystemHealthDto
                {
                    Status = "Healthy",
                    PendingJobs = jobs.Count(j =>
                        j.Status == EnumImportStatus.Uploaded
                        || j.Status == EnumImportStatus.Mapping
                        || j.Status == EnumImportStatus.Approved),
                    RunningJobs = jobs.Count(j => j.Status == EnumImportStatus.Processing),
                    FailedJobs = jobs.Count(j => j.Status == EnumImportStatus.Failed),
                    Note = HealthNote
                });
            }
            catch (Exception ex)
            {
                return Result<SystemHealthDto>.Failure(
                    Error.Internal("Sistem saglik raporu hazirlanirken bir hata olustu.", ex.Message));
            }
        }
    }
}
