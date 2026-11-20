using System;
using AppointMan.Domain.Events.WorkingHour;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class WorkingHour : BaseEntity
    {
        public DayOfWeek DayOfWeek { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan EndTime { get; private set; }
        public bool IsClosed { get; private set; }
        public Guid BusinessId { get; private set; }

        // Navigation Property
        public Business Business { get; private set; }

        private WorkingHour() { } // EF Core

        public WorkingHour(DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid businessId)
    : this(Guid.NewGuid(), dayOfWeek, startTime, endTime, businessId)
        {
        }
        public WorkingHour(Guid id, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid businessId)
    : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            DayOfWeek = dayOfWeek;
            BusinessId = businessId;
            IsClosed = false;

            SetWorkingHours(startTime, endTime);

            AddDomainEvent(new WorkingHourAddedEvent(Id, BusinessId, DayOfWeek, StartTime, EndTime));
        }

        public void SetWorkingHours(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime >= endTime)
                throw new DomainException("Başlangıç saati bitiş saatinden önce olmalıdır.");

            if (startTime.TotalHours < 0 || endTime.TotalHours > 24)
                throw new DomainException("Çalışma saatleri 00:00-24:00 arasında olmalıdır.");

            if (startTime.Minutes % 5 != 0 || endTime.Minutes % 5 != 0)
                throw new DomainException("Çalışma saatleri 5 dakikanın katı olmalıdır.");

            var oldStartTime = StartTime;
            var oldEndTime = EndTime;

            StartTime = startTime;
            EndTime = endTime;
            UpdatedAt = DateTime.UtcNow;

            // Eğer güncelleniyorsa (Id boş değilse) event fırlat
            if (Id != Guid.Empty && (oldStartTime != startTime || oldEndTime != endTime))
            {
                AddDomainEvent(new WorkingHourUpdatedEvent(
                    Id, BusinessId, DayOfWeek,
                    oldStartTime, oldEndTime,
                    startTime, endTime));
            }
        }

        public void Close(string? closedBy = null)
        {
            if (IsClosed)
                return;

            IsClosed = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Open()
        {
            if (!IsClosed)
                return;

            IsClosed = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Delete(string? deletedBy = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new WorkingHourRemovedEvent(Id, BusinessId, DayOfWeek, StartTime, EndTime, deletedBy));
        }

        public bool IsWithinWorkingHours(DateTime dateTime)
        {
            if (IsClosed)
                return false;

            var timeOfDay = dateTime.TimeOfDay;
            return timeOfDay >= StartTime && timeOfDay < EndTime;
        }

        /// <summary>
        /// Araligin tamaminin mesai saatleri icinde kaldigini kontrol eder.
        /// Mesai saatleri isletmenin yerel duvar saatidir; normalization icin
        /// start/end ayni takvim gunune ait olmalidir.
        /// </summary>
        public bool IsWithinWorkingHours(DateTime start, DateTime end)
        {
            if (IsClosed || start >= end)
                return false;

            if (start.TimeOfDay < StartTime)
                return false;

            // 24:00, gunun sonundaki gece yarisidir. Bu sinir degeri yalnizca
            // ayni gunun sonu (veya bir sonraki gunun 00:00'i) olarak kabul edilir.
            if (EndTime >= TimeSpan.FromHours(24))
            {
                if (start.Date == end.Date)
                    return true;

                return end == start.Date.AddDays(1) && end.TimeOfDay == TimeSpan.Zero;
            }

            return start.Date == end.Date && end.TimeOfDay <= EndTime;
        }

        /// <summary>
        /// Isletme genelinde mola tanimlanmaz; molalar calisan bazinda
        /// (EmployeeWorkingHour) tutulur. Aralik API'si domain genelinde ayni
        /// kalsin diye bu metot her zaman false doner.
        /// </summary>
        public bool OverlapsBreak(DateTime start, DateTime end)
        {
            return false;
        }

        public TimeSpan GetDuration()
        {
            return EndTime - StartTime;
        }
    }
}