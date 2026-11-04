using System;
using AppointMan.Domain.Enums;
using AppointMan.Domain.Events.Service;
using AppointMan.Domain.Exceptions;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Domain.Entities
{
    public class Service : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public Money Price { get; private set; }
        public int DurationInMinutes { get; private set; }
        public EnumServiceCategory Category { get; private set; }
        public bool IsActive { get; private set; }
        public Guid BusinessId { get; private set; }

        public Business Business { get; private set; }

        private readonly List<Appointment> _appointments = new();
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        private readonly List<EmployeeService> _employeeServices = new();
        public IReadOnlyCollection<EmployeeService> EmployeeServices => _employeeServices.AsReadOnly();

        private Service() { }

        public Service(string name, Money price, int durationInMinutes,
                      EnumServiceCategory category, Guid businessId)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("İşletme ID boş olamaz.", nameof(businessId));

            SetName(name);
            Price = price ?? throw new ArgumentNullException(nameof(price));
            SetDuration(durationInMinutes);
            Category = category;
            BusinessId = businessId;
            IsActive = true;

            AddDomainEvent(new ServiceCreatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, Category));
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Hizmet adı boş olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Hizmet adı 2-100 karakter arasında olmalıdır.");

            var oldName = Name;
            Name = name;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(oldName) && oldName != name)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, Category,
                    oldName, Price, DurationInMinutes, Category);
                updatedEvent.AddChange("Name", oldName, name);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Açıklama 500 karakterden uzun olamaz.");

            var oldDescription = Description;
            Description = description;
            UpdatedAt = DateTime.UtcNow;

            if (oldDescription != description)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, Category,
                    Name, Price, DurationInMinutes, Category);
                updatedEvent.AddChange("Description", oldDescription ?? string.Empty, description ?? string.Empty);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetDuration(int durationInMinutes)
        {
            if (durationInMinutes < 5 || durationInMinutes > 480)
                throw new DomainException("Hizmet süresi 5 dakika ile 8 saat arasında olmalıdır.");

            if (durationInMinutes % 5 != 0)
                throw new DomainException("Hizmet süresi 5 dakikanın katı olmalıdır.");

            var oldDuration = DurationInMinutes;
            DurationInMinutes = durationInMinutes;
            UpdatedAt = DateTime.UtcNow;

            if (oldDuration != durationInMinutes)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, Category,
                    Name, Price, oldDuration, Category);
                updatedEvent.AddChange("DurationInMinutes", oldDuration, durationInMinutes);
                AddDomainEvent(updatedEvent);
            }
        }

        public void UpdatePrice(Money newPrice)
        {
            if (newPrice == null)
                throw new ArgumentNullException(nameof(newPrice));

            var oldPrice = Price;
            Price = newPrice;
            UpdatedAt = DateTime.UtcNow;

            if (oldPrice != newPrice)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, Category,
                    Name, oldPrice, DurationInMinutes, Category);
                updatedEvent.AddChange("Price", oldPrice, newPrice);
                AddDomainEvent(updatedEvent);
            }
        }

        public void Activate(string? activatedBy = null)
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceActivatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, Category, activatedBy));
        }

        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceDeactivatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, Category, deactivatedBy, reason));
        }

        public void UpdateService(string name, string? description, Money price,
                                 int durationInMinutes, EnumServiceCategory category,
                                 string? updatedBy = null)
        {
            var oldName = Name;
            var oldDescription = Description;
            var oldPrice = Price;
            var oldDuration = DurationInMinutes;
            var oldCategory = Category;

            SetName(name);
            SetDescription(description);
            Price = price ?? throw new ArgumentNullException(nameof(price));
            SetDuration(durationInMinutes);
            Category = category;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new ServiceUpdatedEvent(
                Id, BusinessId, Name, Price, DurationInMinutes, Category,
                oldName, oldPrice, oldDuration, oldCategory);

            if (oldName != name)
                updatedEvent.AddChange("Name", oldName, name);
            if (oldDescription != description)
                updatedEvent.AddChange("Description", oldDescription ?? string.Empty, description ?? string.Empty);
            if (oldPrice != price)
                updatedEvent.AddChange("Price", oldPrice, price);
            if (oldDuration != durationInMinutes)
                updatedEvent.AddChange("DurationInMinutes", oldDuration, durationInMinutes);
            if (oldCategory != category)
                updatedEvent.AddChange("Category", oldCategory, category);

            if (updatedEvent.Changes.Any())
                AddDomainEvent(updatedEvent);
        }

        public void Delete(string? deletedBy = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new ServiceDeletedEvent(Id, BusinessId, Name, Price, DurationInMinutes, Category, deletedBy));
        }

        public bool IsAvailableForAppointment(DateTime appointmentTime)
        {
            return IsActive && !IsDeleted;
        }

        public void AssignToEmployee(EmployeeService employeeService)
        {
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));

            if (employeeService.ServiceId != Id)
                throw new DomainException("Hizmet bu çalışana ait değil.");

            if (_employeeServices.Any(es => es.EmployeeId == employeeService.EmployeeId && !es.IsDeleted))
                throw new DomainException("Bu hizmet zaten çalışana atanmış.");

            _employeeServices.Add(employeeService);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnassignFromEmployee(EmployeeService employeeService)
        {
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));

            if (!_employeeServices.Contains(employeeService))
                throw new DomainException("Bu hizmet çalışanda bulunamadı.");

            _employeeServices.Remove(employeeService);
            UpdatedAt = DateTime.UtcNow;
        }

        public IReadOnlyCollection<Employee> GetAssignedEmployees()
        {
            return _employeeServices
                .Where(es => !es.IsDeleted && es.IsActive && es.Employee != null)
                .Select(es => es.Employee)
                .ToList()
                .AsReadOnly();
        }
    }
}