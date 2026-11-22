using System;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Events.Service;
using Rezervise.Domain.Exceptions;
using Rezervise.Domain.ValueObjects;

namespace Rezervise.Domain.Entities
{
    public class Service : BaseEntity
    {
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public Money? Price { get; private set; }
        public int DurationInMinutes { get; private set; }
        public bool IsActive { get; private set; }
        public Guid BusinessId { get; private set; }

        public Guid ServiceCategoryId { get; private set; }
        public ServiceCategory ServiceCategory { get; private set; }

        public Business Business { get; private set; }

        private readonly List<Appointment> _appointments = new();
        public IReadOnlyCollection<Appointment> Appointments => _appointments.AsReadOnly();

        private readonly List<EmployeeService> _employeeServices = new();
        public IReadOnlyCollection<EmployeeService> EmployeeServices => _employeeServices.AsReadOnly();

        private Service() { }

        public Service(string name, Money price, int durationInMinutes,
              Guid serviceCategoryId, Guid businessId)
    : this(Guid.NewGuid(), name, price, durationInMinutes, serviceCategoryId, businessId)
        {
        }
        public Service(Guid id, string name, Money price, int durationInMinutes,
              Guid serviceCategoryId, Guid businessId)
    : base(id)
        {
            if (businessId == Guid.Empty)
                throw new ArgumentException("Isletme ID bos olamaz.", nameof(businessId));

            if (serviceCategoryId == Guid.Empty)
                throw new ArgumentException("Kategori ID bos olamaz.", nameof(serviceCategoryId));

            SetName(name);
            Price = price ?? throw new ArgumentNullException(nameof(price));
            SetDuration(durationInMinutes);
            ServiceCategoryId = serviceCategoryId;
            BusinessId = businessId;
            IsActive = true;

            AddDomainEvent(new ServiceCreatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId));
        }
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Hizmet adi bos olamaz.");

            if (name.Length < 2 || name.Length > 100)
                throw new DomainException("Hizmet adi 2-100 karakter arasinda olmalidir.");

            var oldName = Name;
            Name = name;
            UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(oldName) && oldName != name)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);
                updatedEvent.AddChange("Name", oldName, name);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new DomainException("Açiklama 500 karakterden uzun olamaz.");

            var oldDescription = Description;
            Description = description;
            UpdatedAt = DateTime.UtcNow;

            if (oldDescription != description)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);
                updatedEvent.AddChange("Description", oldDescription ?? string.Empty, description ?? string.Empty);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetDuration(int durationInMinutes)
        {
            if (durationInMinutes < 5 || durationInMinutes > 480)
                throw new DomainException("Hizmet süresi 5 dakika ile 8 saat arasinda olmalidir.");

            if (durationInMinutes % 5 != 0)
                throw new DomainException("Hizmet süresi 5 dakikanin kati olmalidir.");

            var oldDuration = DurationInMinutes;
            DurationInMinutes = durationInMinutes;
            UpdatedAt = DateTime.UtcNow;

            if (oldDuration != durationInMinutes)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);
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
                    Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);
                updatedEvent.AddChange("Price", oldPrice, newPrice);
                AddDomainEvent(updatedEvent);
            }
        }

        public void SetServiceCategory(ServiceCategory serviceCategory)
        {
            if (serviceCategory == null)
                throw new ArgumentNullException(nameof(serviceCategory));

            if (!serviceCategory.IsLeaf())
                throw new DomainException("Hizmet sadece leaf kategoriye atanabilir.");

            var oldCategoryId = ServiceCategoryId;
            ServiceCategory = serviceCategory;
            ServiceCategoryId = serviceCategory.Id;
            UpdatedAt = DateTime.UtcNow;

            if (oldCategoryId != ServiceCategoryId)
            {
                var updatedEvent = new ServiceUpdatedEvent(
                    Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);
                updatedEvent.AddChange("ServiceCategoryId", oldCategoryId.ToString(), ServiceCategoryId.ToString());
                AddDomainEvent(updatedEvent);
            }
        }

        public void Activate(string? activatedBy = null)
        {
            if (IsActive)
                return;

            IsActive = true;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceActivatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId, activatedBy));
        }

        public void Deactivate(string? deactivatedBy = null, string? reason = null)
        {
            if (!IsActive)
                return;

            IsActive = false;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new ServiceDeactivatedEvent(Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId, deactivatedBy, reason));
        }

        public void UpdateService(string name, string? description, Money price,
                                 int durationInMinutes, ServiceCategory serviceCategory,
                                 string? updatedBy = null)
        {
            if (serviceCategory == null)
                throw new ArgumentNullException(nameof(serviceCategory));

            if (!serviceCategory.IsLeaf())
                throw new DomainException("Hizmet sadece leaf kategoriye atanabilir.");

            var oldName = Name;
            var oldDescription = Description;
            var oldPrice = Price;
            var oldDuration = DurationInMinutes;
            var oldCategoryId = ServiceCategoryId;

            SetName(name);
            SetDescription(description);
            Price = price ?? throw new ArgumentNullException(nameof(price));
            SetDuration(durationInMinutes);
            ServiceCategory = serviceCategory;
            ServiceCategoryId = serviceCategory.Id;
            UpdatedAt = DateTime.UtcNow;

            var updatedEvent = new ServiceUpdatedEvent(
                Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId);

            if (oldName != name)
                updatedEvent.AddChange("Name", oldName, name);
            if (oldDescription != description)
                updatedEvent.AddChange("Description", oldDescription ?? string.Empty, description ?? string.Empty);
            if (oldPrice != price)
                updatedEvent.AddChange("Price", oldPrice, price);
            if (oldDuration != durationInMinutes)
                updatedEvent.AddChange("DurationInMinutes", oldDuration, durationInMinutes);
            if (oldCategoryId != ServiceCategoryId)
                updatedEvent.AddChange("ServiceCategoryId", oldCategoryId.ToString(), ServiceCategoryId.ToString());

            if (updatedEvent.Changes.Any())
                AddDomainEvent(updatedEvent);
        }

        public void Delete(string? deletedBy = null)
        {
            if (IsDeleted)
                return;

            SoftDelete();
            AddDomainEvent(new ServiceDeletedEvent(Id, BusinessId, Name, Price, DurationInMinutes, ServiceCategoryId, deletedBy));
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
                throw new DomainException("Hizmet bu çalisana ait degil.");

            if (_employeeServices.Any(es => es.EmployeeId == employeeService.EmployeeId && !es.IsDeleted))
                throw new DomainException("Bu hizmet zaten çalisana atanmis.");

            _employeeServices.Add(employeeService);
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnassignFromEmployee(EmployeeService employeeService)
        {
            if (employeeService == null)
                throw new ArgumentNullException(nameof(employeeService));

            if (!_employeeServices.Contains(employeeService))
                throw new DomainException("Bu hizmet çalisanda bulunamadi.");

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