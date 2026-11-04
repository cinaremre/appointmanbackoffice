using AppointMan.Domain.Exceptions;

namespace AppointMan.Domain.Entities
{
    public class EmployeeService : BaseEntity
    {
        public Guid EmployeeId { get; private set; }
        public Employee Employee { get; private set; }

        public Guid ServiceId { get; private set; }
        public Service Service { get; private set; }

        public decimal? CustomPrice { get; private set; }
        public int? CustomDurationInMinutes { get; private set; }
        public string? Note { get; private set; }
        public bool IsActive { get; private set; }

        private EmployeeService() { }

        public EmployeeService(Guid employeeId, Guid serviceId)
        {
            if (employeeId == Guid.Empty)
                throw new ArgumentException("Calisan ID bos olamaz.", nameof(employeeId));

            if (serviceId == Guid.Empty)
                throw new ArgumentException("Hizmet ID bos olamaz.", nameof(serviceId));

            EmployeeId = employeeId;
            ServiceId = serviceId;
            IsActive = true;
        }

        public void SetCustomPrice(decimal? price)
        {
            if (price.HasValue && price.Value < 0)
                throw new DomainException("Ozel fiyat negatif olamaz.");

            if (price.HasValue && price.Value > 99999999.99m)
                throw new DomainException("Ozel fiyat cok yuksek.");

            CustomPrice = price.HasValue ? Math.Round(price.Value, 2) : null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetCustomDuration(int? durationInMinutes)
        {
            if (durationInMinutes.HasValue)
            {
                if (durationInMinutes.Value < 5 || durationInMinutes.Value > 480)
                    throw new DomainException("Sure 5-480 dakika arasinda olmalidir.");

                if (durationInMinutes.Value % 5 != 0)
                    throw new DomainException("Sure 5 dakikanin kati olmalidir.");
            }

            CustomDurationInMinutes = durationInMinutes;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetNote(string? note)
        {
            if (note != null && note.Length > 500)
                throw new DomainException("Not 500 karakterden uzun olamaz.");

            Note = note;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public decimal GetEffectivePrice(decimal serviceDefaultPrice)
        {
            return CustomPrice ?? serviceDefaultPrice;
        }

        public int GetEffectiveDuration(int serviceDefaultDuration)
        {
            return CustomDurationInMinutes ?? serviceDefaultDuration;
        }
    }
}