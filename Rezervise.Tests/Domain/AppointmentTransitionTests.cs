using Rezervise.Domain.Entities;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Exceptions;

namespace Rezervise.Tests.Domain;

public sealed class AppointmentTransitionTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Pending_Confirm_ThenComplete_FollowsSingleMatrix()
    {
        var appointment = NewAppointment(Now.AddHours(3), EnumAppointmentStatus.Pending);

        Assert.True(appointment.CanTransitionTo(EnumAppointmentStatus.Confirmed, Now));
        appointment.Confirm("owner", Now);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);

        Assert.False(appointment.CanTransitionTo(EnumAppointmentStatus.Completed, Now));
        appointment.Complete("owner", Now.AddHours(4));
        Assert.Equal(EnumAppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void TerminalStates_BlockEveryTransition()
    {
        var completed = NewAppointment(Now.AddHours(-1), EnumAppointmentStatus.Completed);
        var cancelled = NewAppointment(Now.AddHours(2), EnumAppointmentStatus.Cancelled);

        Assert.False(Appointment.IsValidStatusTransition(EnumAppointmentStatus.Completed, EnumAppointmentStatus.Confirmed));
        Assert.False(Appointment.IsValidStatusTransition(EnumAppointmentStatus.Cancelled, EnumAppointmentStatus.Pending));
        Assert.Throws<DomainException>(() => completed.Confirm("owner", Now));
        Assert.Throws<DomainException>(() => cancelled.Reschedule(Now.AddDays(1), "owner", Now));
    }

    [Fact]
    public void Reschedule_AllowedFromConfirmed_ButNotFromNoShow()
    {
        var confirmed = NewAppointment(Now.AddDays(1), EnumAppointmentStatus.Confirmed);
        confirmed.Reschedule(Now.AddDays(2), "owner", Now);

        Assert.Equal(EnumAppointmentStatus.Pending, confirmed.Status);

        var noShow = NewAppointment(Now.AddHours(-2), EnumAppointmentStatus.NoShow);
        Assert.False(noShow.CanTransitionTo(EnumAppointmentStatus.Pending, Now));
        Assert.Throws<DomainException>(() => noShow.Reschedule(Now.AddDays(1), "owner", Now));
    }

    [Fact]
    public void NoShow_RequiresPastAppointmentAndActiveStatus()
    {
        var future = NewAppointment(Now.AddHours(2), EnumAppointmentStatus.Confirmed);
        Assert.False(future.CanTransitionTo(EnumAppointmentStatus.NoShow, Now));
        Assert.Throws<DomainException>(() => future.MarkAsNoShow("System", Now));

        var past = NewAppointment(Now.AddHours(-1), EnumAppointmentStatus.Confirmed);
        Assert.True(past.CanTransitionTo(EnumAppointmentStatus.NoShow, Now));
        past.MarkAsNoShow("System", Now);
        Assert.Equal(EnumAppointmentStatus.NoShow, past.Status);
    }

    [Fact]
    public void Cancel_UsesMatrixAndLeadTime()
    {
        // Domain yalnızca durum matrisini ve geçmiş tarih kuralını uygular.
        // 2 saatlik müşteri sınırı handler'larda IsWithinCustomerCutoff ile kontrol edilir.
        var tooSoon = NewAppointment(Now.AddMinutes(30), EnumAppointmentStatus.Confirmed);
        Assert.True(tooSoon.CanTransitionTo(EnumAppointmentStatus.Cancelled, Now));
        Assert.True(tooSoon.IsWithinCustomerCutoff(Now));

        var inTime = NewAppointment(Now.AddHours(4), EnumAppointmentStatus.Pending);
        Assert.False(inTime.IsWithinCustomerCutoff(Now));
        Assert.True(inTime.CanTransitionTo(EnumAppointmentStatus.Cancelled, Now));
        inTime.Cancel("test", "customer", Now);
        Assert.Equal(EnumAppointmentStatus.Cancelled, inTime.Status);
    }

    private static Appointment NewAppointment(DateTime date, EnumAppointmentStatus status)
    {
        var appointment = TestData.NewAppointment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            date,
            status,
            endTime: date.AddMinutes(30));

        return appointment;
    }
}
