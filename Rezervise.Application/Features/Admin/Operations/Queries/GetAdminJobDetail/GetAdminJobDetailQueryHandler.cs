using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobDetail
{
    /// <summary>
    /// Aktarim isinin detayini dondurur. Satir sonuclari ilk 200 ile
    /// sinirlandirilir; rapor buyuk dosyalarda yoruma acik kalsin diye
    /// tum satirlar tasinmaz.
    /// </summary>
    public class GetAdminJobDetailQueryHandler
        : IRequestHandler<GetAdminJobDetailQuery, Result<AdminJobDetailDto>>
    {
        /// <summary>Detayda tasinacak azami satir sayisi.</summary>
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
                // 1. Is kontrolu.
                var job = await _jobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<AdminJobDetailDto>.Failure(
                        Error.NotFoundById("Aktarim isi", request.ImportJobId));

                // 2. Satir sonuclari: satir numarasina gore sirali, ilk 200.
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
                    Error.Internal("Aktarim isi detayi getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
