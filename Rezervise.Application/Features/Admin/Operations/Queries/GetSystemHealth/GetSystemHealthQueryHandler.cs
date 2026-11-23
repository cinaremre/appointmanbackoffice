using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetSystemHealth
{
    /// <summary>
    /// Sistem sağlık raporu (doküman Modül 20).
    ///
    /// BASITLESTIRME (ilk sürüm): DbReachable gibi gerçek bağlantı testleri
    /// yerine uygulamanın ayakta öldüğü bilgisi + aktarım is durum sayıları
    /// raporlanır. Sağlık kontrolleri sonraki sürümlerde Redis, mesaj brokeri
    /// ve bildirim sağlayıcısı gibi bileşimler için genişletilecektir.
    /// </summary>
    public class GetSystemHealthQueryHandler
        : IRequestHandler<GetSystemHealthQuery, Result<SystemHealthDto>>
    {
        /// <summary>Sağlık kapsamı açıklaması.</summary>
        private const string HealthNote =
            "İlk sürüm sağlık raporu: uygulamanın ayakta öldüğü ve aktarım işlerinin durum sayıları raporlanır. Veritabanı, Redis, mesaj brokeri ve sağlayıcı sağlık kontrolleri sonraki sürümlerde eklenecek.";

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
                // Sorgu döngüsünü tamamlıyorsa uygulama ve veri katmanı ayaktadır.
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
                    Error.Internal("Sistem sağlık raporu hazırlanırken bir hata oluştu.", ex.Message));
            }
        }
    }
}
