using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>
    /// Aktarım işinin detayını döndürür. Satır sonuçları ilk 200 ile
    /// sınırlandırılır; rapor büyük dosyalarda yoruma açık kalsın diye
    /// tüm satırlar taşınmaz.
    /// </summary>
    public class GetAdminJobDetailQueryHandler
        : IRequestHandler<GetAdminJobDetailQuery, Result<AdminJobDetailDto>>
    {
        /// <summary>Detayda taşınacak azami satır sayısı.</summary>
        private const int MaxRows = 200;

        private readonly IRepository<ImportJob> _jobRepository;
        private readonly IRepository<ImportRowResult> _rowRepository;

        public GetAdminJobDetailQueryHandler(
            IRepository<ImportJob> jobRepository,
            IRepository<ImportRowResult> rowRepository)
        {
            _jobRepository = jobRepository;
            _rowRepository = rowRepository;
        }

        public async Task<Result<AdminJobDetailDto>> Handle(
            GetAdminJobDetailQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. İs kontrolü.
                var job = await _jobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<AdminJobDetailDto>.Failure(
                        Error.NotFoundById("Aktarım isi", request.ImportJobId));

                // 2. Satır sonuçları: satır numarasına göre sıralı, ilk 200.
                var rows = await _rowRepository.FindAsync(
                    r => r.ImportJobId == job.Id && !r.IsDeleted,
                    cancellationToken);

                var rowDtos = rows
                    .OrderBy(r => r.RowNumber)
                    .Take(MaxRows)
                    .Select(r => new AdminJobRowDto
                    {
                        Id = r.Id,
                        RowNumber = r.RowNumber,
                        SourceRecordKey = r.SourceRecordKey,
                        Status = r.Status,
                        ErrorDetails = r.ErrorDetails
                    })
                    .ToList();

                return Result<AdminJobDetailDto>.Success(new AdminJobDetailDto
                {
                    Job = GetAdminJobsQueryHandler.ToDto(job),
                    Rows = rowDtos
                });
            }
            catch (Exception ex)
            {
                return Result<AdminJobDetailDto>.Failure(
                    Error.Internal("Aktarım isi detayı getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
