using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Domain.ValueObjects;

namespace AppointMan.Tests;

public static class TestData
{
    public static Business NewBusiness(Guid ownerId, Guid? id = null)
    {
        var business = new Business(
            id ?? Guid.NewGuid(),
            "Test Isletmesi",
            default(EnumBusinessType),
            new Address(34, null, "Test Sokak", "34000"),
            new PhoneNumber("+905550000000"),
            new Email("business@test.com"),
            ownerId);

        business.ClearDomainEvents();
        return business;
    }

    public static Employee NewEmployee(Guid businessId, Guid? id = null)
    {
        var employee = new Employee(
            id ?? Guid.NewGuid(),
            "Test",
            "Calisan",
            new PhoneNumber("+905550000001"),
            businessId);

        employee.ClearDomainEvents();
        return employee;
    }

    public static Service NewService(Guid businessId, Guid id, int durationMinutes = 30)
    {
        var service = new Service(
            id,
            "Test Hizmeti",
            new Money(100m),
            durationMinutes,
            Guid.NewGuid(),
            businessId);

        service.ClearDomainEvents();
        return service;
    }

    public static void AddEmployeeService(Employee employee, Guid serviceId)
        => employee.AddService(new EmployeeService(employee.Id, serviceId));

    public static void AddEmployeeWorkingHour(
        Employee employee,
        DayOfWeek day,
        TimeSpan start,
        TimeSpan end,
        TimeSpan? breakStart = null,
        TimeSpan? breakEnd = null)
    {
        var workingHour = new EmployeeWorkingHour(employee.Id, day, start, end);
        if (breakStart.HasValue && breakEnd.HasValue)
            workingHour.SetBreakTime(breakStart, breakEnd);

        employee.AddWorkingHour(workingHour);
    }

    public static void AddBusinessWorkingHour(
        Business business,
        DayOfWeek day,
        TimeSpan start,
        TimeSpan end)
        => business.AddWorkingHour(new WorkingHour(day, start, end, business.Id));

    public static Appointment NewAppointment(
        Guid id,
        Guid userId,
        Guid businessId,
        Guid serviceId,
        Guid employeeId,
        DateTime appointmentDate,
        EnumAppointmentStatus status = EnumAppointmentStatus.Pending,
        DateTime? endTime = null)
    {
        var appointment = new Appointment(
            id,
            userId,
            businessId,
            serviceId,
            employeeId,
            appointmentDate,
            notes: null,
            validateDate: false);

        appointment.SetStatusForSeed(status, endTime: endTime);
        appointment.ClearDomainEvents();
        return appointment;
    }

    public static User NewUser(Guid id, EnumUserRole role = EnumUserRole.Customer)
    {
        var user = new User(
            id,
            "Test",
            "Kullanici",
            new Email($"user-{id:N}@test.com"),
            new PhoneNumber("+905550000002"),
            role);

        user.ClearDomainEvents();
        return user;
    }

    public static Review NewReview(
        Guid id,
        Guid userId,
        Guid businessId,
        Guid appointmentId,
        int rating = 5,
        string comment = "Hizmet gercekten cok iyiydi.")
    {
        var review = new Review(id, userId, businessId, rating, comment, appointmentId);
        review.ClearDomainEvents();
        return review;
    }
}
