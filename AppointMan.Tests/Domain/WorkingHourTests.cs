using AppointMan.Domain.Entities;
using AppointMan.Tests;

namespace AppointMan.Tests.Domain;

public class WorkingHourTests
{
    [Fact]
    public void IsOpenOn_ShouldUseTimeOfDay_NotMidnight()
    {
        var business = TestData.NewBusiness(Guid.NewGuid());
        TestData.AddBusinessWorkingHour(business, DayOfWeek.Monday, TimeSpan.FromHours(9), TimeSpan.FromHours(18));

        var monday = new DateTime(2026, 11, 23, 14, 0, 0, DateTimeKind.Unspecified);

        Assert.True(business.IsOpenOn(monday));
        Assert.True(business.IsOpenDuring(monday, monday.AddHours(1)));
        Assert.False(business.IsOpenDuring(monday.AddHours(17), monday.AddHours(19)));
    }

    [Fact]
    public void EmployeeWorkingHour_ShouldRejectBreakOverlap()
    {
        var employee = TestData.NewEmployee(Guid.NewGuid());
        TestData.AddEmployeeWorkingHour(
            employee,
            DayOfWeek.Tuesday,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(18),
            TimeSpan.FromHours(12),
            TimeSpan.FromHours(13));

        var workingHour = employee.GetWorkingHourByDay(DayOfWeek.Tuesday)!;
        var beforeBreak = new DateTime(2026, 11, 24, 11, 30, 0);
        var overlapsBreak = new DateTime(2026, 11, 24, 12, 30, 0);

        Assert.True(workingHour.IsWithinWorkingHours(beforeBreak, beforeBreak.AddMinutes(30)));
        Assert.True(workingHour.OverlapsBreak(overlapsBreak, overlapsBreak.AddMinutes(30)));
        Assert.False(employee.IsWorkingDuring(overlapsBreak, overlapsBreak.AddMinutes(30)));
    }
}
