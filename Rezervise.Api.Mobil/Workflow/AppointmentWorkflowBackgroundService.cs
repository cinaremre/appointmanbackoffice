using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Domain.Common;
using Rezervise.Domain.Enums;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;

namespace Rezervise.Api.Workflow
{
    /// <summary>
    /// Randevu zaman akışını arka planda tarar: yaklaşan onaylı randevular için
    /// hatırlatma gönderir, süresi geçen onaylı randevuları tamamlar, süresi
    /// geçen bekleyen randevuları gelmedi olarak işaretler.
    /// </summary>
    public sealed class AppointmentWorkflowBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailQueue _emailQueue;
        private readonly ILogger<AppointmentWorkflowBackgroundService> _logger;

        public AppointmentWorkflowBackgroundService(
            IServiceScopeFactory scopeFactory,
            IEmailQueue emailQueue,
            ILogger<AppointmentWorkflowBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(WorkflowPolicy.SweepInterval);

            // İlk tarama açılışta yapılır; sonra belirli aralıklarla tekrarlanır.
            do
            {
                try
                {
                    await SweepAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Tek bir tarama hatası servisi durdurmaz.
                    _logger.LogError(ex, "Randevu iş akışı taraması başarısız oldu.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task SweepAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var appointments = provider.GetRequiredService<IRepository<Appointment>>();
            var users = provider.GetRequiredService<IRepository<User>>();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            var dateTime = provider.GetRequiredService<IDateTimeService>();

            var now = dateTime.UtcNow;
            var sweepStart = now - WorkflowPolicy.AutoCompleteGracePeriod;
            var sweepEnd = now + WorkflowPolicy.ReminderLeadTime;

            var candidates = await appointments.FindAsync(
                a => !a.IsDeleted &&
                     (a.Status == EnumAppointmentStatus.Pending || a.Status == EnumAppointmentStatus.Confirmed) &&
                     a.AppointmentDate >= sweepStart &&
                     a.AppointmentDate <= sweepEnd,
                cancellationToken);

            var changed = false;

            foreach (var appointment in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (appointment.Status == EnumAppointmentStatus.Confirmed &&
                    !appointment.IsReminderSent &&
                    appointment.AppointmentDate > now &&
                    appointment.AppointmentDate <= now + WorkflowPolicy.ReminderLeadTime)
                {
                    appointment.MarkReminderSent(now);
                    await EnqueueReminderAsync(users, appointment, cancellationToken);
                    changed = true;
                    continue;
                }

                if (appointment.AppointmentDate > sweepStart)
                    continue;

                if (appointment.Status == EnumAppointmentStatus.Confirmed &&
                    appointment.AppointmentDate <= now - WorkflowPolicy.AutoCompleteGracePeriod)
                {
                    appointment.Complete("System", now);
                    changed = true;
                }
                else if (appointment.Status == EnumAppointmentStatus.Pending &&
                         appointment.AppointmentDate <= now - WorkflowPolicy.AutoCompleteGracePeriod)
                {
                    appointment.MarkAsNoShow("System", now);
                    changed = true;
                }
            }

            if (changed)
                await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task EnqueueReminderAsync(
            IRepository<User> users,
            Appointment appointment,
            CancellationToken cancellationToken)
        {
            var user = await users.GetByIdAsync(appointment.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
                return;

            var when = appointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm");

            _emailQueue.TryEnqueue(new EmailMessage
            {
                To = user.Email,
                Subject = "Randevunuz yaklaşıyor",
                HtmlBody = $"<html><body><h2>Randevunuz yaklaşıyor</h2><p>{when} randevunuzu hatırlatmak istedik.</p></body></html>",
                TextBody = $"{when} randevunuzu hatırlatmak istedik."
            });
        }
    }
}
