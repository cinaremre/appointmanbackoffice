using AppointMan.Application.Common.Diagnostics;
using AppointMan.Application.Common.Interfaces;
using AppointMan.Application.Common.Results;
using AppointMan.Application.Features.Admin.Appointments.Commands.CancelAppointmentByAdmin;
using AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentConflicts;
using AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointmentDetail;
using AppointMan.Application.Features.Admin.Appointments.Queries.GetAdminAppointments;
using AppointMan.Application.Features.Appointments.Commands.CancelAppointment;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Enums;
using AppointMan.Tests.Fakes;
using MediatR;

namespace AppointMan.Tests.Application.BackOffice;

/// <summary>
/// Backoffice randevu operasyonlari (dokuman Modul 12). Yonetici iptali,
/// mevcut domain iptal handler'ina delege edilerek test edilir; MediatR
/// mock'u yerine gercek handler'i saran FakeMediator kullanilir.
/// </summary>
public class AdminAppointmentTests
{
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid BusinessId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static readonly DateTime NowUtc = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);

    // ===== Denetim kaydi fake'leri =====

    /// <summary>Denetim kaydinin yakalandigi bellek ici kayit.</summary>
    private sealed class AuditEntry
    {
        public Guid ActorUserId { get; set; }
        public string? ActorRole { get; set; }
        public Guid? BusinessId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Reason { get; set; }
        public string? CorrelationId { get; set; }
        public string? IpAddress { get; set; }
    }

    /// <summary>IAdminAuditSink fake'i: kayitlari listeye alir, hata uretmez.</summary>
    private sealed class FakeAdminAuditSink : IAdminAuditSink
    {
        public List<AuditEntry> Entries { get; } = new();

        public Task RecordAsync(
            Guid actorUserId,
            string actorRole,
            Guid? businessId,
            string entityType,
            string entityId,
            string action,
            string? oldValues = null,
            string? newValues = null,
            string? reason = null,
            string? correlationId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            Entries.Add(new AuditEntry
            {
                ActorUserId = actorUserId,
                ActorRole = actorRole,
                BusinessId = businessId,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Reason = reason,
                CorrelationId = correlationId,
                IpAddress = ipAddress
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>Sabit correlation ve IP ureten denetim baglami fake'i.</summary>
    private sealed class FakeAdminAuditContext : IAdminAuditContext
    {
        public string CorrelationId => "test-correlation";
        public string IpAddress => "127.0.0.1";
    }

    /// <summary>
    /// IMediator fake'i: mock kutuphanesi yerine verilen delegate'i
    /// calistirir. Testlerde delegate, gercek CancelAppointmentCommandHandler'i
    /// sarmalar; boylece yonetici iptali domain akisini dogru sekilde tetikler.
    /// </summary>
    private sealed class FakeMediator : IMediator
    {
        private readonly Func<object, Task<object>> _send;

        public FakeMediator(Func<object, Task<object>> send)
        {
            _send = send;
        }

        /// <summary>Delegate'e gonderilen istekler; dogrulama icin tutulur.</summary>
        public List<object> SentRequests { get; } = new();

        public async Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return (TResponse)await _send(request);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
            => throw new NotSupportedException("Yanitsiz istekler bu testte kullanilmaz.");

        public async Task<object> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return await _send(request);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Stream istekleri bu testte kullanilmaz.");

        public IAsyncEnumerable<object> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Stream istekleri bu testte kullanilmaz.");

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }

    // ===== Yardimcilar =====

    private static TestCurrentUserService AdminUser()
        => TestCurrentUserService.For(AdminId, "Admin");

    private static (FakeAdminAuditSink Sink, FakeAdminAuditContext Context) CreateAudit()
        => (new FakeAdminAuditSink(), new FakeAdminAuditContext());

    /// <summary>
    /// Yonetici iptalini, gercek CancelAppointmentCommandHandler'i saran
    /// FakeMediator ile kurar. Domain kontrolu tek kaynaktan yurur.
    /// </summary>
    private static (CancelAppointmentByAdminCommandHandler Handler, FakeMediator Mediator) CreateCancelSut(
        FakeRepository<Appointment> appointmentRepo,
        FakeAdminAuditSink sink,
        FakeAdminAuditContext context)
    {
        var currentUser = AdminUser();
        var innerHandler = new CancelAppointmentCommandHandler(
            appointmentRepo,
            new StubDateTimeService(NowUtc),
            new FakeUnitOfWork(),
            new AppointManMetrics(),
            currentUser);

        var mediator = new FakeMediator(async request =>
        {
            if (request is CancelAppointmentCommand command)
            {
                var innerResult = await innerHandler.Handle(command, CancellationToken.None);
                return (object)innerResult;
            }

            throw new NotSupportedException($"Beklenmeyen istek tipi: {request.GetType().Name}");
        });

        var handler = new CancelAppointmentByAdminCommandHandler(
            appointmentRepo,
            mediator,
            currentUser,
            sink,
            context);

        return (handler, mediator);
    }

    private static Appointment NewAppointment(
        Guid id,
        Guid employeeId,
        DateTime appointmentDate,
        EnumAppointmentStatus status = EnumAppointmentStatus.Confirmed)
        => TestData.NewAppointment(id, CustomerId, BusinessId, ServiceId, employeeId, appointmentDate, status);

    // ===== Yonetici iptali =====

    [Fact]
    public async Task CancelByAdmin_ShouldDelegateToDomainHandlerAndRecordAudit()
    {
        var appointmentId = Guid.NewGuid();
        var appointmentDate = NowUtc.AddHours(3);
        var appointment = NewAppointment(appointmentId, EmployeeId, appointmentDate);
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(appointmentRepo, sink, context);

        var result = await sut.Handle(
            new CancelAppointmentByAdminCommand
            {
                AppointmentId = appointmentId,
                Reason = "Musteri destek talebi uzerine",
                CancellationReason = "Isletme karari ile iptal edildi"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(appointmentId, result.Value);
        Assert.Equal(EnumAppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("Isletme karari ile iptal edildi", appointment.CancellationReason);

        // Domain komutu dogru parametrelerle mediator'a gitmeli.
        var sentCommand = Assert.IsType<CancelAppointmentCommand>(Assert.Single(mediator.SentRequests));
        Assert.Equal(appointmentId, sentCommand.AppointmentId);
        Assert.Equal(CustomerId, sentCommand.UserId);
        Assert.Equal("Isletme karari ile iptal edildi", sentCommand.Reason);
        Assert.Equal($"Admin:{AdminId}", sentCommand.CancelledBy);

        // Basari sonrasi denetim kaydi yazilmali.
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("CancelAppointment", entry.Action);
        Assert.Equal("Appointment", entry.EntityType);
        Assert.Equal(appointmentId.ToString(), entry.EntityId);
        Assert.Equal(BusinessId, entry.BusinessId);
        Assert.Equal("Musteri destek talebi uzerine", entry.Reason);
        Assert.Equal(AdminId, entry.ActorUserId);
        Assert.Contains("Confirmed", entry.OldValues, StringComparison.Ordinal);
        Assert.Contains("Cancelled", entry.NewValues, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CancelByAdmin_WhenDomainRejects_ShouldReturnSameErrorWithoutAudit()
    {
        var appointmentId = Guid.NewGuid();
        // Gecmis randevu: domain kurallari iptale izin vermez.
        var appointment = NewAppointment(appointmentId, EmployeeId, NowUtc.AddHours(-2));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(appointmentRepo, sink, context);

        var result = await sut.Handle(
            new CancelAppointmentByAdminCommand
            {
                AppointmentId = appointmentId,
                Reason = "Yonetici karari"
            },
            CancellationToken.None);

        // Domain hatasi aynen dondurulmeli; denetim kaydi yazilmamali.
        Assert.True(result.IsFailure);
        Assert.Equal("BUSINESS_RULE_VIOLATION", result.Error!.Code);
        Assert.Contains("iptal", result.Error.Message, StringComparison.Ordinal);
        Assert.Equal(EnumAppointmentStatus.Confirmed, appointment.Status);
        Assert.Single(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CancelByAdmin_WithEmptyReason_ShouldReturnValidationError()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, EmployeeId, NowUtc.AddHours(3));
        var appointmentRepo = new FakeRepository<Appointment>(new[] { appointment });

        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(appointmentRepo, sink, context);

        var result = await sut.Handle(
            new CancelAppointmentByAdminCommand { AppointmentId = appointmentId, Reason = "  " },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("VALIDATION_ERROR", result.Error!.Code);
        // Gerekce olmadigindan domain'e hicbir istek gitmez.
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    [Fact]
    public async Task CancelByAdmin_WhenAppointmentMissing_ShouldReturnNotFound()
    {
        var (sink, context) = CreateAudit();
        var (sut, mediator) = CreateCancelSut(new FakeRepository<Appointment>(), sink, context);

        var result = await sut.Handle(
            new CancelAppointmentByAdminCommand
            {
                AppointmentId = Guid.NewGuid(),
                Reason = "Yonetici karari"
            },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
        Assert.Empty(mediator.SentRequests);
        Assert.Empty(sink.Entries);
    }

    // ===== Listeleme =====

    [Fact]
    public async Task GetAdminAppointments_ShouldFilterByStatusAndDateRange()
    {
        var tomorrow = NowUtc.Date.AddDays(1);
        var a1 = NewAppointment(Guid.NewGuid(), EmployeeId, tomorrow.AddHours(10), EnumAppointmentStatus.Confirmed);
        var a2 = NewAppointment(Guid.NewGuid(), EmployeeId, tomorrow.AddHours(11), EnumAppointmentStatus.Pending);
        var a3 = NewAppointment(Guid.NewGuid(), EmployeeId, NowUtc.Date.AddHours(-2), EnumAppointmentStatus.Cancelled);

        // Baska isletmenin onayli randevusu: isletme filtresinde disarida kalmali.
        var a4 = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, Guid.NewGuid(), ServiceId, Guid.NewGuid(),
            tomorrow.AddHours(12), EnumAppointmentStatus.Confirmed);

        var sut = new GetAdminAppointmentsQueryHandler(
            new FakeRepository<Appointment>(new[] { a1, a2, a3, a4 }));

        // 1. Durum + tarih araligi filtresi: yarinin onayli randevulari.
        var result = await sut.Handle(
            new GetAdminAppointmentsQuery
            {
                BusinessId = BusinessId,
                Status = EnumAppointmentStatus.Confirmed,
                FromDate = tomorrow,
                ToDate = tomorrow.AddDays(1)
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(a1.Id, item.Id);

        // 2. Calisan filtresi.
        var byEmployee = await sut.Handle(
            new GetAdminAppointmentsQuery { EmployeeId = EmployeeId, Page = 1, PageSize = 50 },
            CancellationToken.None);
        Assert.Equal(3, byEmployee.Value!.TotalCount);

        // 3. Filtresiz: tum randevular.
        var all = await sut.Handle(
            new GetAdminAppointmentsQuery { Page = 1, PageSize = 50 },
            CancellationToken.None);
        Assert.Equal(4, all.Value!.TotalCount);
    }

    [Fact]
    public async Task GetAdminAppointments_ShouldClampPageSize()
    {
        var appointments = Enumerable.Range(0, 3)
            .Select(i => NewAppointment(Guid.NewGuid(), EmployeeId, NowUtc.AddHours(i + 1)))
            .ToList();

        var sut = new GetAdminAppointmentsQueryHandler(new FakeRepository<Appointment>(appointments));

        var result = await sut.Handle(
            new GetAdminAppointmentsQuery { Page = 0, PageSize = 5000 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Page);
        Assert.Equal(GetAdminAppointmentsQueryHandler.MaxPageSize, result.Value.PageSize);
    }

    // ===== Detay =====

    [Fact]
    public async Task GetAdminAppointmentDetail_ShouldReturnNewestFirstLimitedHistory()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = NewAppointment(appointmentId, EmployeeId, NowUtc.AddHours(3));

        // 25 gecmis kaydi: en yenisi en basta olmali, en fazla 20 kayit donmeli.
        var histories = Enumerable.Range(0, 25).Select(i =>
        {
            var history = new AppointmentHistory(
                appointmentId,
                EnumAppointmentStatus.Pending,
                EnumAppointmentStatus.Confirmed,
                changedBy: "test",
                changeReason: $"Degisiklik {i}",
                changedAt: NowUtc.AddMinutes(i));
            return history;
        }).ToList();

        var sut = new GetAdminAppointmentDetailQueryHandler(
            new FakeRepository<Appointment>(new[] { appointment }),
            new FakeRepository<AppointmentHistory>(histories));

        var result = await sut.Handle(
            new GetAdminAppointmentDetailQuery { AppointmentId = appointmentId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal(appointmentId, dto.Id);
        Assert.Equal(GetAdminAppointmentDetailQueryHandler.HistoryLimit, dto.History.Count);

        // Siralama: azalan degisiklik zamani; ilk kayit en yeni (i=24).
        var ordered = dto.History.Select(h => h.ChangedAt).ToList();
        Assert.Equal(ordered.OrderByDescending(d => d), ordered);
        Assert.Equal(NowUtc.AddMinutes(24), dto.History[0].ChangedAt);
        Assert.Equal("Degisiklik 24", dto.History[0].ChangeReason);
    }

    [Fact]
    public async Task GetAdminAppointmentDetail_WhenMissing_ShouldReturnNotFound()
    {
        var sut = new GetAdminAppointmentDetailQueryHandler(
            new FakeRepository<Appointment>(),
            new FakeRepository<AppointmentHistory>());

        var result = await sut.Handle(
            new GetAdminAppointmentDetailQuery { AppointmentId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NOT_FOUND", result.Error!.Code);
    }

    // ===== Cakisma tespiti =====

    [Fact]
    public async Task GetAdminAppointmentConflicts_ShouldDetectSameEmployeeSameTime()
    {
        var conflictTime = NowUtc.Date.AddDays(1).AddHours(10);
        var otherEmployee = Guid.NewGuid();

        // Ayni calisan + ayni saat: cakisma.
        var dup1 = NewAppointment(Guid.NewGuid(), EmployeeId, conflictTime);
        var dup2 = NewAppointment(Guid.NewGuid(), EmployeeId, conflictTime, EnumAppointmentStatus.Pending);

        // Farkli saat veya farkli calisan: cakisma degil.
        var differentTime = NewAppointment(Guid.NewGuid(), EmployeeId, conflictTime.AddHours(1));
        var differentEmployee = NewAppointment(Guid.NewGuid(), otherEmployee, conflictTime);

        // Iptal edilmis randevu aktif sayilmaz.
        var cancelled = NewAppointment(
            Guid.NewGuid(), EmployeeId, conflictTime, EnumAppointmentStatus.Cancelled);

        // Baska isletmenin randevusu: kapsam disi.
        var otherBusiness = TestData.NewAppointment(
            Guid.NewGuid(), CustomerId, Guid.NewGuid(), ServiceId, EmployeeId, conflictTime);

        var sut = new GetAdminAppointmentConflictsQueryHandler(
            new FakeRepository<Appointment>(new[] { dup1, dup2, differentTime, differentEmployee, cancelled, otherBusiness }));

        var result = await sut.Handle(
            new GetAdminAppointmentConflictsQuery { BusinessId = BusinessId, Date = conflictTime },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var conflict = Assert.Single(result.Value!);
        Assert.Equal(EmployeeId, conflict.EmployeeId);
        Assert.Equal(conflictTime, conflict.AppointmentDate);
        Assert.Equal(2, conflict.Count);
        Assert.Equal(new[] { dup1.Id, dup2.Id }.OrderBy(id => id), conflict.AppointmentIds);
    }

    [Fact]
    public async Task GetAdminAppointmentConflicts_WhenNoOverlap_ShouldReturnEmptyList()
    {
        var appointment = NewAppointment(Guid.NewGuid(), EmployeeId, NowUtc.Date.AddDays(1).AddHours(10));
        var sut = new GetAdminAppointmentConflictsQueryHandler(
            new FakeRepository<Appointment>(new[] { appointment }));

        var result = await sut.Handle(
            new GetAdminAppointmentConflictsQuery { BusinessId = BusinessId, Date = NowUtc.Date.AddDays(1) },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}
