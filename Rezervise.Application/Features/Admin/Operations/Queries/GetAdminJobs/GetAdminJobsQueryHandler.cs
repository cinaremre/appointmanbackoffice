using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Admin.Operations.Queries.GetAdminJobs
{
    /// <summary>
    /// Aktarim islerini filtreler, olusturma zamanina gore tersten siralar ve
    /// sayfalara ayirir.
    /// </summary>
    public class GetAdminJobsQueryHandler
        : IRequestHandler<GetAdminJobsQuery, Result<AdminJobListDto>>
    {
        private readonly IRepository<ImportJob> _jobRepository;

        public GetAdminJobsQueryHandler(IRepository<ImportJob> jobRepository)
        {
            _jobRepository = jobRepository;
        }

        public async Task<Result<AdminJobListDto>> Handle(
            GetAdminJobsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var businessId = request.BusinessId;
                var status = request.Status;

                var jobs = await _jobRepository.FindAsync(
                    j => !j.IsDeleted
                        && (businessId == null || j.BusinessId == businessId)
                        && (status == null || j.Status == status),
                    cancellationToken);

                var page = AdminPagination.ClampPage(request.Page);
                var pageSize = AdminPagination.ClampPageSize(request.PageSize);

                var ordered = jobs
                    .OrderByDescending(j => j.CreatedAt)
                    .ToList();

                var items = ordered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ToDto)
                    .ToList();

                return Result<AdminJobListDto>.Success(new AdminJobListDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = ordered.Count
                });
            }
            catch (Exception ex)
            {
                return Result<AdminJobListDto>.Failure(
                    Error.Internal("Aktarim isi listesi getirilirken bir hata olustu.", ex.Message));
            }
        }

        /// <summary>ImportJob entity'sinden yonetici gorunumu uretir.</summary>
        internal static AdminJobDto ToDto(ImportJob job) => new()
        {
            Id = job.Id,
            BusinessId = job.BusinessId,
            SourceSystem = job.SourceSystem,
            Status = job.Status,
            TotalRows = job.TotalRows,
            ImportedRows = job.ImportedRows,
            FailedRows = job.FailedRows,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }
}
