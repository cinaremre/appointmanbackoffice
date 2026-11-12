using System.Diagnostics.Metrics;

namespace AppointMan.Application.Common.Diagnostics
{
    /// <summary>
    /// Central business metric instruments for AppointMan.
    /// Instruments carry fully qualified names so the exported Prometheus series are
    /// unambiguous, for example <c>appointman_appointments_created_total</c>.
    /// </summary>
    /// <remarks>
    /// Labels are deliberately low cardinality. Identifiers such as user, business or
    /// appointment ids must never be attached: each distinct value creates a new time series
    /// and would overload the metrics backend in production.
    /// </remarks>
    public sealed class AppointManMetrics : IDisposable
    {
        /// <summary>Meter name used by the OpenTelemetry pipeline.</summary>
        public const string MeterName = "AppointMan";

        /// <summary>Meter version reported to the exporter.</summary>
        public const string MeterVersion = "1.0.0";

        private const string RoleTag = "role";
        private const string ResultTag = "result";
        private const string OutcomeTag = "outcome";

        private readonly Meter _meter;

        private readonly Counter<long> _appointmentsCreated;
        private readonly Counter<long> _appointmentsConfirmed;
        private readonly Counter<long> _appointmentsCancelled;
        private readonly Counter<long> _appointmentsCompleted;
        private readonly Counter<long> _appointmentsRescheduled;
        private readonly Counter<long> _usersRegistered;
        private readonly Counter<long> _loginAttempts;
        private readonly Counter<long> _refreshTokenRotations;

        /// <summary>
        /// Creates the meter and all business instruments.
        /// </summary>
        public AppointManMetrics()
        {
            _meter = new Meter(MeterName, MeterVersion);

            _appointmentsCreated = _meter.CreateCounter<long>(
                "appointman.appointments.created",
                unit: "{appointment}",
                description: "Number of appointments successfully booked.");

            _appointmentsConfirmed = _meter.CreateCounter<long>(
                "appointman.appointments.confirmed",
                unit: "{appointment}",
                description: "Number of appointments confirmed by a business owner.");

            _appointmentsCancelled = _meter.CreateCounter<long>(
                "appointman.appointments.cancelled",
                unit: "{appointment}",
                description: "Number of appointments cancelled.");

            _appointmentsCompleted = _meter.CreateCounter<long>(
                "appointman.appointments.completed",
                unit: "{appointment}",
                description: "Number of appointments marked as completed.");

            _appointmentsRescheduled = _meter.CreateCounter<long>(
                "appointman.appointments.rescheduled",
                unit: "{appointment}",
                description: "Number of appointments rescheduled to a new time slot.");

            _usersRegistered = _meter.CreateCounter<long>(
                "appointman.users.registered",
                unit: "{user}",
                description: "Number of self service registrations.");

            _loginAttempts = _meter.CreateCounter<long>(
                "appointman.auth.login.attempts",
                unit: "{attempt}",
                description: "Login attempts by outcome.");

            _refreshTokenRotations = _meter.CreateCounter<long>(
                "appointman.auth.refresh.rotations",
                unit: "{rotation}",
                description: "Refresh token rotation attempts by outcome.");
        }

        /// <summary>Records a successfully created appointment.</summary>
        public void RecordAppointmentCreated() => _appointmentsCreated.Add(1);

        /// <summary>Records a confirmed appointment.</summary>
        public void RecordAppointmentConfirmed() => _appointmentsConfirmed.Add(1);

        /// <summary>Records a cancelled appointment.</summary>
        public void RecordAppointmentCancelled() => _appointmentsCancelled.Add(1);

        /// <summary>Records a completed appointment.</summary>
        public void RecordAppointmentCompleted() => _appointmentsCompleted.Add(1);

        /// <summary>Records a rescheduled appointment.</summary>
        public void RecordAppointmentRescheduled() => _appointmentsRescheduled.Add(1);

        /// <summary>
        /// Records a successful self service registration.
        /// </summary>
        /// <param name="role">Role granted at registration; limited to Customer or BusinessOwner.</param>
        public void RecordUserRegistered(string role)
            => _usersRegistered.Add(1, new KeyValuePair<string, object?>(RoleTag, role));

        /// <summary>
        /// Records a login attempt.
        /// </summary>
        /// <param name="result">Outcome: <c>success</c>, <c>invalid_credentials</c> or <c>locked</c>.</param>
        public void RecordLoginAttempt(string result)
            => _loginAttempts.Add(1, new KeyValuePair<string, object?>(ResultTag, result));

        /// <summary>
        /// Records a refresh token rotation attempt.
        /// </summary>
        /// <param name="outcome">Outcome: <c>success</c>, <c>reused</c>, <c>invalid</c> or <c>expired</c>.</param>
        public void RecordRefreshTokenRotation(string outcome)
            => _refreshTokenRotations.Add(1, new KeyValuePair<string, object?>(OutcomeTag, outcome));

        /// <inheritdoc />
        public void Dispose() => _meter.Dispose();
    }
}
