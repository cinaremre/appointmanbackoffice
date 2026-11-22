using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Queries.GetByBusiness
{
    public class GetWorkingHoursByBusinessQueryHandler : IRequestHandler<GetWorkingHoursByBusinessQuery, Result<List<WorkingHourDto>>>
    {
        private readonly IRepository<WorkingHour> _workingHourRepository;

        public GetWorkingHoursByBusinessQueryHandler(IRepository<WorkingHour> workingHourRepository)
        {
            _workingHourRepository = workingHourRepository;
        }

        public async Task<Result<List<WorkingHourDto>>> Handle(GetWorkingHoursByBusinessQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Isletmenin tum calisma saatlerini getir
                var workingHours = await _workingHourRepository.FindAsync(
                    w => w.BusinessId == request.BusinessId && !w.IsDeleted,
                    cancellationToken);

                // 2. DTO'ya donustur
                var dtos = workingHours.Select(w => new WorkingHourDto
                {
                    Id = w.Id,
                    BusinessId = w.BusinessId,
                    DayOfWeek = w.DayOfWeek,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    IsClosed = w.IsClosed,
                    // DayOfWeek.ToString() framework'un Ingilizce adini doner; Turkce ad helper'dan alinir.
                    DayName = DayNameHelper.ToTurkishName(w.DayOfWeek)
                }).ToList();

                return Result<List<WorkingHourDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                return Result<List<WorkingHourDto>>.Failure(
                    Error.Internal("Calisma saatleri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}