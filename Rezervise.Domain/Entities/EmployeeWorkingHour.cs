using Rezervise.Domain.Exceptions;

namespace Rezervise.Domain.Entities
{
    public class EmployeeWorkingHour : BaseEntity
    {
        public Guid EmployeeId { get; private set; }
        public Employee Employee { get; private set; }

        public DayOfWeek DayOfWeek { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }

        public TimeSpan? BreakStartTime { get; private set; }
        public TimeSpan? BreakEndTime { get; private set; }

        public bool IsClosed { get; private set; }

        private EmployeeWorkingHour() { }

        public EmployeeWorkingHour(Guid employeeId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    : this(Guid.NewGuid(), employeeId, dayOfWeek, startTime, endTime)
        {
        }
        public EmployeeWorkingHour(Guid id, Guid employeeId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    : base(id)
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException("Çalışan ID boş olamaz.", nameof(employeeId));

            EmployeeId = employeeId;
            DayOfWeek = dayOfWeek;
            IsClosed = false;

            SetWorkingHours(startTime, endTime);
        }

        public void SetWorkingHours(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new DomainException("Başlangıç saati bitiş saatinden önce olmalıdır.");

            if (startTime.TotalHours < 0 || endTime.TotalHours > 24)
                throw new DomainException("Saatler 00:00-24:00 arasında olmalıdır.");

            if (startTime.Minutes % 5 != 0 || endTime.Minutes % 5 != 0)
                throw new DomainException("Saatler 5 dakikanın katı olmalıdır.");

            StartTime = startTime;
            EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetBreakTime(TimeSpan? breakStart, TimeSpan? breakEnd)
        {
            if (breakStart.HasValue && breakEnd.HasValue)
            {
                if (breakStart.Value >= breakEnd.Value)
                    throw new DomainException("Mola başlangıcı mola bitişinden önce olmalıdır.");

                if (breakStart.Value < StartTime || breakEnd.Value > EndTime)
                    throw new DomainException("Mola, çalışma saatleri içinde olmalıdır.");

                if (breakStart.Value.Minutes % 5 != 0 || breakEnd.Value.Minutes % 5 != 0)
                    throw new DomainException("Mola saatleri 5 dakikanın katı olmalıdır.");
            }

            BreakStartTime = breakStart;
            BreakEndTime = breakEnd;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Close()
        {
            IsClosed = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Open()
        {
            IsClosed = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsWithinWorkingHours(DateTime dateTime)
        {
            if (IsClosed)
                return false;

            var timeOfDay = dateTime.TimeOfDay;
            return timeOfDay >= StartTime && timeOfDay < EndTime;
        }

        public bool IsWithinBreakTime(DateTime dateTime)
        {
            if (!BreakStartTime.HasValue || !BreakEndTime.HasValue)
                return false;

            var timeOfDay = dateTime.TimeOfDay;
            return timeOfDay >= BreakStartTime.Value && timeOfDay < BreakEndTime.Value;
        }

        /// <summary>
        /// Aralığın tamamının çalışma saatleri içinde kaldığını kontrol eder.
        /// Saatler işletmenin yerel duvar saatidir; aralık aynı takvim gününde olmalıdır.
        /// </summary>
        public bool IsWithinWorkingHours(DateTime start, DateTime end)
        {
            if (IsClosed || start >= end)
                return false;

            if (start.TimeOfDay < StartTime)
                return false;

            // 24:00 bitiş saati gün sonunu ifade eder; gece yarısı sınırına izin verilir.
            if (EndTime >= TimeSpan.FromHours(24))
            {
                if (start.Date == end.Date)
                    return true;

                return end == start.Date.AddDays(1) && end.TimeOfDay == TimeSpan.Zero;
            }

            return start.Date == end.Date && end.TimeOfDay <= EndTime;
        }

        /// <summary>
        /// Aralığın mola ile yarım-açık (start dahil, end hariç) olarak
        /// kesişip kesmediğini kontrol eder.
        /// </summary>
        public bool OverlapsBreak(DateTime start, DateTime end)
        {
            if (!BreakStartTime.HasValue || !BreakEndTime.HasValue || start >= end)
                return false;

            // 24:00 bitişinde end bir sonraki günün 00:00'idir; aynı is günü sayılır.
            var isMidnightEnd = end == start.Date.AddDays(1) && end.TimeOfDay == TimeSpan.Zero;
            if (start.Date != end.Date && !isMidnightEnd)
                return false;

            return start.TimeOfDay < BreakEndTime.Value &&
                   (end.TimeOfDay > BreakStartTime.Value || isMidnightEnd);
        }

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }

        public TimeSpan? GetBreakDuration()
        {
            if (!BreakStartTime.HasValue || !BreakEndTime.HasValue)
                return null;

            return BreakEndTime.Value - BreakStartTime.Value;
        }

        public TimeSpan GetNetWorkingDuration()
        {
            var total = GetDuration();
            var breakTime = GetBreakDuration() ?? TimeSpan.Zero;
            return total - breakTime;
        }

        public void Delete()
        {
            if (IsDeleted) return;

            SoftDelete();
        }
    }
}