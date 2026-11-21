using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Failed durumundaki isleri ve satir orneklerini toplar. Destek ekiplerinin
    /// hizli taramasi icin ornek boyutu ilk 50 satirla sinirlidir.
    /// </summary>
    public class GetAdminFailedOperationsQueryHandler
        : IRequestHandler<GetAdminFailedOperationsQuery, Result<AdminFailedOperationsDto>>
    {
        /// <summary>Raporda tasinacak azami basarisiz satir ornegi.</summary>
        private const int MaxRowSamples = 50;

        private readonly IRepository<ImportJob> _jobRepository;
        private readonly IRepository<ImportRowResult> _rowRepository;

        public GetAdminFailedOperationsQueryHandler(
            IRepository<ImportJob> jobRepository,
            IRepository<ImportRowResult> rowRepository)
        {
            _jobRepository = jobRepository;
            _rowRepository = rowRepository;
        }

        public async Task<Result<AdminFailedOperationsDto>> Handle(
            GetAdminFailedOperationsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Failed durumundaki isler.
                var failedJobs = await _jobRepository.FindAsync(
                    j => !j.IsDeleted && j.Status == EnumImportStatus.Failed,
                    cancellationToken);

                // 2. Failed satir ornekleri.
                var failedRows = await _rowRepository.FindAsync(
                    r => !r.IsDeleted && r.Status == EnumImportRowStatus.Failed,
                    cancellationToken);

                return Result<AdminFailedOperationsDto>.Success(new AdminFailedOperationsDto
                {
                    FailedJobs = failedJobs
                        .OrderByDescending(j => j.CompletedAt ?? j.CreatedAt)
                        .Select(GetAdminJobsQueryHandler.ToDto)
                        .ToList(),
                    FailedRows = failedRows
                        .OrderBy(r => r.ImportJobId)
                        .ThenBy(r => r.RowNumber)
                        .Take(MaxRowSamples)
                        .Select(r => new AdminFailedRowDto
                        {
                            ImportJobId = r.ImportJobId,
                            RowNumber = r.RowNumber,
                            SourceRecordKey = r.SourceRecordKey,
                            ErrorDetails = r.ErrorDetails
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                return Result<AdminFailedOperationsDto>.Failure(
                    Error.Internal("Basarisiz operasyonlar getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
