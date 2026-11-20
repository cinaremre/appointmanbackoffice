using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests;

namespace AppointMan.Tests.Domain;

public class AppointmentDomainTests
{
    [Fact]
    public void OverlapsWith_ShouldUseEndTime_WhenPresent()
    {
        var start = new DateTime(2026, 11, 23, 9, 0, 0, DateTimeKind.Utc);
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            start, endTime: start.AddHours(2));

        Assert.True(appointment.OverlapsWith(
            start.AddHours(1), start.AddHours(3), 30));
        Assert.False(appointment.OverlapsWith(
            start.AddHours(2), start.AddHours(3), 30));
    }

    [Fact]
    public void OverlapsWith_ShouldUseFallbackDuration_WhenEndTimeMissing()
    {
        var start = new DateTime(2026, 11, 23, 9, 0, 0, DateTimeKind.Utc);
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            start);

        Assert.True(appointment.OverlapsWith(start.AddMinutes(20), start.AddMinutes(40), 30));
        Assert.False(appointment.OverlapsWith(start.AddMinutes(30), start.AddMinutes(40), 30));
    }

    [Fact]
    public void Business_ShouldRejectIntervalEndingAfterClosingTime()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));
        var monday = new DateTime(2026, 11, 23, 17, 45, 0);

        Assert.True(business.IsOpenDuring(monday, monday.AddMinutes(15)));
        Assert.False(business.IsOpenDuring(monday, monday.AddMinutes(30)));
    }

    [Fact]
    public void Employee_ShouldRejectUnapprovedTimeOff()
    {
        var employee = TestData.NewEmployee(Guid.NewGuid());
        var monday = new DateTime(2026, 11, 23, 10, 0, 0);
        var unapproved = new EmployeeTimeOff(
            employee.Id, monday, monday.AddHours(2), EnumTimeOffType.Vacation);
        employee.AddTimeOff(unapproved);

        Assert.False(employee.IsOnApprovedTimeOff(monday, monday.AddHours(1)));

        unapproved.Approve("owner");
        Assert.True(employee.IsOnApprovedTimeOff(monday, monday.AddHours(1)));
    }
}
