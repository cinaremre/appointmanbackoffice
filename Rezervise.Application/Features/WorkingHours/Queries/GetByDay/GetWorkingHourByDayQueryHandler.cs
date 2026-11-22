using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Helpers;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.WorkingHours.Queries.GetByDay
{
    public class GetWorkingHourByDayQueryHandler : IRequestHandler<GetWorkingHourByDayQuery, Result<WorkingHourDto>>
    {
        private readonly IRepository<WorkingHour> _workingHourRepository;

        public GetWorkingHourByDayQueryHandler(IRepository<WorkingHour> workingHourRepository)
        {
            _workingHourRepository = workingHourRepository;
        }

        public async Task<Result<WorkingHourDto>> Handle(GetWorkingHourByDayQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Belirli bir gunun calisma saatini getir
                var workingHours = await _workingHourRepository.FindAsync(
                    w => w.BusinessId == request.BusinessId && w.DayOfWeek == request.Day && !w.IsDeleted,
                    cancellationToken);

                var workingHour = workingHours.FirstOrDefault();

                if (workingHour is null)
                    return Result<WorkingHourDto>.Failure(Error.NotFound("Calisma saati", $"{request.BusinessId}-{request.Day}"));

                // 2. DTO'ya donustur
                var dto = new WorkingHourDto
                {
                    Id = workingHour.Id,
                    BusinessId = workingHour.BusinessId,
                    DayOfWeek = workingHour.DayOfWeek,
                    StartTime = workingHour.StartTime,
                    EndTime = workingHour.EndTime,
                    IsClosed = workingHour.IsClosed,
                    // DayOfWeek.ToString() framework'un Ingilizce adini doner; Turkce ad helper'dan alinir.
                    DayName = DayNameHelper.ToTurkishName(workingHour.DayOfWeek)
                };

                return Result<WorkingHourDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<WorkingHourDto>.Failure(Error.Internal("Calisma saati getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}