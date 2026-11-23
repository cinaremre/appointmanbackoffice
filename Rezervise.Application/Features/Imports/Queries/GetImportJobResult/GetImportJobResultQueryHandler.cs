using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Imports.Queries.GetImportJobResult
{
    public class GetImportJobResultQueryHandler : IRequestHandler<GetImportJobResultQuery, Result<ImportJobResultDto>>
    {
        private readonly IRepository<ImportJob> _importJobRepository;
        private readonly IRepository<ImportRowResult> _rowResultRepository;
        private readonly IRepository<Business> _businessRepository;
        private readonly ICurrentUserService _currentUser;

        public GetImportJobResultQueryHandler(
            IRepository<ImportJob> importJobRepository,
            IRepository<ImportRowResult> rowResultRepository,
            IRepository<Business> businessRepository,
            ICurrentUserService currentUser)
        {
            _importJobRepository = importJobRepository;
            _rowResultRepository = rowResultRepository;
            _businessRepository = businessRepository;
            _currentUser = currentUser;
        }

        public async Task<Result<ImportJobResultDto>> Handle(GetImportJobResultQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. İs kontrolü
                var job = await _importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<ImportJobResultDto>.Failure(Error.NotFound("Aktarım isi", request.ImportJobId.ToString()));

                // 2. Yetki kontrolü: işin işletmesi üzerinden JWT kimliği esas alınır.
                var business = await _businessRepository.GetByIdAsync(job.BusinessId, cancellationToken);
                if (business is null)
                    return Result<ImportJobResultDto>.Failure(Error.NotFound("İşletme", job.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<ImportJobResultDto>.Failure(Error.Unauthorized("Bu aktarım işinin sonucunu görüntüleme yetkiniz yok."));

                // 3. Satır sonuçlarını getir.
                var rows = await _rowResultRepository.FindAsync(
                    r => r.ImportJobId == job.Id && !r.IsDeleted,
                    cancellationToken);

                // 4. DTO oluştur.
                var dto = new ImportJobResultDto
                {
                    ImportJobId = job.Id,
                    BusinessId = job.BusinessId,
                    SourceSystem = job.SourceSystem,
                    FileName = job.FileName,
                    Status = job.Status,
                    TotalRows = job.TotalRows,
                    ImportedRows = job.ImportedRows,
                    FailedRows = job.FailedRows,
                    SkippedRows = rows.Count(r => r.Status == EnumImportRowStatus.SkippedDuplicate),
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    Rows = rows
                        .OrderBy(r => r.RowNumber)
                        .Select(r => new ImportRowResultDto
                        {
                            RowNumber = r.RowNumber,
                            SourceRecordKey = r.SourceRecordKey,
                            Status = r.Status,
                            ErrorDetails = r.ErrorDetails
                        })
                        .ToList()
                };

                return Result<ImportJobResultDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<ImportJobResultDto>.Failure(
                    Error.Internal("Aktarım sonucu getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}
