using AppointMan.Domain.Enums;
using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class EmployeeTimeOff : BaseEntity
    {
        public Guid EmployeeId { get; private set; }
        public Employee Employee { get; private set; }

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public EnumTimeOffType Type { get; private set; }
        public string? Reason { get; private set; }
        public bool IsApproved { get; private set; }
        public string? ApprovedBy { get; private set; }

        private EmployeeTimeOff() { }

        public EmployeeTimeOff(
            Guid employeeId,
            DateTime startDate,
            DateTime endDate,
            EnumTimeOffType type,
            string? reason = null)
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException("Calisan ID bos olamaz.", nameof(employeeId));

            if (startDate >= endDate)
                throw new DomainException("Baslangic tarihi bitis tarihinden once olmalidir.");

            EmployeeId = employeeId;
            StartDate = startDate;
            EndDate = endDate;
            Type = type;
            Reason = reason;
            IsApproved = false;
        }

        public void Approve(string approvedBy)
        {
            if (IsApproved)
                throw new DomainException("Izin zaten onaylanmis.");

            IsApproved = true;
            ApprovedBy = approvedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject()
        {
            if (IsApproved)
                throw new DomainException("Onaylanmis izin reddedilemez.");

            SoftDelete();
        }

        public void SetReason(string? reason)
        {
            if (reason != null && reason.Length > 500)
                throw new DomainException("Sebep 500 karakterden uzun olamaz.");

            Reason = reason;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsDateWithinTimeOff(DateTime dateTime)
        {
            return dateTime >= StartDate && dateTime <= EndDate;
        }

        public bool OverlapsWith(DateTime start, DateTime end)
        {
            return StartDate < end && EndDate > start;
        }

        public int GetTotalDays()
        {
            return (EndDate.Date - StartDate.Date).Days + 1;
        }
    }
}