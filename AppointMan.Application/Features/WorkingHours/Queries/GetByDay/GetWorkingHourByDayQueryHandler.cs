using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.WorkingHours.Queries.GetByDay
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
                    DayName = workingHour.DayOfWeek.ToString()
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