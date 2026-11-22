using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;
using Rezervise.Tests;

namespace Rezervise.Tests.Domain;

/// <summary>
/// Randevu "gelmedi" (no-show) isaretleme ve musteri degisiklik siniri (2 saat) kurallari.
/// </summary>
public class AppointmentNoShowDomainTests
{
    private static readonly DateTime Now = new(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkAsNoShow_ShouldSwitchStatus_WhenAppointmentAlreadyStarted()
    {
        var appointment = NewAppointment(Now.AddHours(-1), EnumAppointmentStatus.Confirmed);

        appointment.MarkAsNoShow("owner", Now);

        Assert.Equal(EnumAppointmentStatus.NoShow, appointment.Status);
        Assert.True(appointment.IsNoShow);
        Assert.Equal(Now, appointment.UpdatedAt);
    }

    [Fact]
    public void MarkAsNoShow_ShouldWriteHistoryEntry()
    {
        var appointment = NewAppointment(Now.AddHours(-1), EnumAppointmentStatus.Pending);

        appointment.MarkAsNoShow("owner", Now);

        var lastChange = appointment.GetLastChange();

        Assert.NotNull(lastChange);
        Assert.Equal(EnumAppointmentStatus.Pending, lastChange!.OldStatus);
        Assert.Equal(EnumAppointmentStatus.NoShow, lastChange.NewStatus);
        Assert.Equal("Randevuya gelinmedi", lastChange.ChangeReason);
    }

    [Fact]
    public void MarkAsNoShow_ShouldThrow_WhenAppointmentIsInFuture()
    {
        var appointment = NewAppointment(Now.AddHours(1), EnumAppointmentStatus.Confirmed);

        Assert.Throws<DomainException>(() => appointment.MarkAsNoShow("owner", Now));
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void MarkAsNoShow_ShouldThrow_WhenAppointmentIsAlreadyCancelled()
    {
        var appointment = NewAppointment(Now.AddHours(-1), EnumAppointmentStatus.Cancelled);

        Assert.Throws<DomainException>(() => appointment.MarkAsNoShow("owner", Now));
        Assert.Equal(EnumAppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public void IsWithinCustomerCutoff_ShouldBeTrue_WhenLessThanTwoHoursRemain()
    {
        var appointment = NewAppointment(Now.AddHours(1), EnumAppointmentStatus.Confirmed);

        Assert.True(appointment.IsWithinCustomerCutoff(Now));
    }

    [Fact]
    public void IsWithinCustomerCutoff_ShouldBeFalse_WhenMoreThanTwoHoursRemain()
    {
        var appointment = NewAppointment(Now.AddHours(3), EnumAppointmentStatus.Confirmed);

        Assert.False(appointment.IsWithinCustomerCutoff(Now));
    }

    [Fact]
    public void CustomerChangeCutoffHours_ShouldBeTwo()
    {
        Assert.Equal(2, Appointment.CustomerChangeCutoffHours);
    }

    private static Appointment NewAppointment(DateTime appointmentDate, EnumAppointmentStatus status)
        => TestData.NewAppointment(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            appointmentDate, status);
}
