using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminFailedOperations
{
    /// <summary>
    /// Failed durumundaki işleri ve satır örneklerini toplar. Destek ekiplerinin
    /// hızlı taraması için örnek boyutu ilk 50 satırla sınırlıdır.
    /// </summary>
    public class GetAdminFailedOperationsQueryHandler
        : IRequestHandler<GetAdminFailedOperationsQuery, Result<AdminFailedOperationsDto>>
    {
        /// <summary>Raporda taşınacak azami başarısız satır örneği.</summary>
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
                // 1. Failed durumundaki işler.
                var failedJobs = await _jobRepository.FindAsync(
                    j => !j.IsDeleted && j.Status == EnumImportStatus.Failed,
                    cancellationToken);

                // 2. Failed satır örnekleri.
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
                    Error.Internal("Başarısız operasyonlar getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
