using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Imports.Queries.GetImportJobResult
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
                // 1. Is kontrolu
                var job = await _importJobRepository.GetByIdAsync(request.ImportJobId, cancellationToken);
                if (job is null)
                    return Result<ImportJobResultDto>.Failure(Error.NotFound("Aktarim isi", request.ImportJobId.ToString()));

                // 2. Yetki kontrolu: isin isletmesi uzerinden JWT kimligi esas alinir.
                var business = await _businessRepository.GetByIdAsync(job.BusinessId, cancellationToken);
                if (business is null)
                    return Result<ImportJobResultDto>.Failure(Error.NotFound("Isletme", job.BusinessId.ToString()));

                if (!CallerAuthorization.CanActForBusiness(_currentUser, business.OwnerId))
                    return Result<ImportJobResultDto>.Failure(Error.Unauthorized("Bu aktarim isinin sonucunu goruntuleme yetkiniz yok."));

                // 3. Satir sonuclarini getir.
                var rows = await _rowResultRepository.FindAsync(
                    r => r.ImportJobId == job.Id && !r.IsDeleted,
                    cancellationToken);

                // 4. DTO olustur.
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
                    Error.Internal("Aktarim sonucu getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}
