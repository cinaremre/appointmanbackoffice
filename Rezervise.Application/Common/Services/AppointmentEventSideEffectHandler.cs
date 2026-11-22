using System.Text.RegularExpressions;
using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;
using Rezervise.Domain.Events;
using Rezervise.Domain.Events.Appointment;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Common.Services
{
    /// <summary>
    /// Randevu domain event'lerini gerçek iş akışı yan etkilerine bağlar:
    /// yapılandırılmış denetim kaydı, müşteriye e-posta kuyruğu ve mobil push
    /// kuyruğu. Event kimliği ile dedupe yapılır; aynı olay iki kez yan etki üretmez.
    /// </summary>
    public sealed class AppointmentEventSideEffectHandler : INotificationHandler<DomainEventNotification>
    {
        private static readonly Regex HtmlTagPattern = new("<[^>]+>", RegexOptions.Compiled);

        private readonly IEmailQueue _emailQueue;
        private readonly IPushQueue _pushQueue;
        private readonly IRepository<Domain.Entities.User> _users;
        private readonly IRepository<Domain.Entities.DeviceToken> _deviceTokens;
        private readonly IWorkflowAuditSink _audit;
        private readonly EventSideEffectDeduplicator _deduplicator;

        public AppointmentEventSideEffectHandler(
            IEmailQueue emailQueue,
            IPushQueue pushQueue,
            IRepository<Domain.Entities.User> users,
            IRepository<Domain.Entities.DeviceToken> deviceTokens,
            IWorkflowAuditSink audit,
            EventSideEffectDeduplicator deduplicator)
        {
            _emailQueue = emailQueue;
            _pushQueue = pushQueue;
            _users = users;
            _deviceTokens = deviceTokens;
            _audit = audit;
            _deduplicator = deduplicator;
        }

        public async Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            if (!_deduplicator.Enabled)
                return;

            if (domainEvent is not AppointmentEventBase && domainEvent is not AppointmentStatusChangedEvent)
                return;

            if (!_deduplicator.TryRegister(domainEvent.Id))
                return;

            _audit.Record(
                domainEvent.GetType().Name,
                domainEvent.Id,
                GetAggregateId(domainEvent),
                GetSummary(domainEvent));

            var plan = BuildEmailPlan(domainEvent);
            if (plan is null)
                return;

            var user = await _users.GetByIdAsync(plan.UserId, cancellationToken);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
                return;

            _emailQueue.TryEnqueue(new EmailMessage
            {
                To = user.Email,
                Subject = plan.Subject,
                HtmlBody = plan.HtmlBody,
                TextBody = plan.TextBody
            });

            // Mobil push: ayni icerigin kisa bicimi kuyruga verilir. Cihazi
            // olmayan kullaniciya hic kuyruk yazilmaz; gonderim kuyruga alinir,
            // istek thread'i saglayici beklemez.
            var tokens = await _deviceTokens.FindAsync(
                t => t.UserId == user.Id && t.IsActive,
                cancellationToken);

            foreach (var token in tokens)
            {
                _pushQueue.TryEnqueue(new PushMessage
                {
                    Token = token.Token,
                    UserId = user.Id,
                    Title = plan.Subject,
                    // HTML govdeden etiketleri soyarak kisa bildirim metni uret.
                    Body = ToPlainBody(plan.HtmlBody)
                });
            }
        }

        /// <summary>E-posta govdesinden push icin kisa duz metin uretir.</summary>
        private static string ToPlainBody(string htmlBody)
        {
            var plain = HtmlTagPattern.Replace(htmlBody, " ").Trim();
            return plain.Length > 160 ? plain[..160] : plain;
        }

        private static Guid? GetAggregateId(IDomainEvent domainEvent)
        {
            return domainEvent switch
            {
                AppointmentEventBase e => e.AppointmentId,
                AppointmentStatusChangedEvent e => e.AppointmentId,
                _ => null
            };
        }

        private static string GetSummary(IDomainEvent domainEvent)
        {
            return domainEvent switch
            {
                AppointmentCreatedEvent e => $"Olusturuldu: {e.AppointmentDate:dd.MM.yyyy HH:mm}",
                AppointmentConfirmedEvent e => $"Onaylandi: {e.AppointmentDate:dd.MM.yyyy HH:mm}",
                AppointmentCancelledEvent e => $"Iptal edildi: {e.AppointmentDate:dd.MM.yyyy HH:mm}",
                AppointmentRescheduledEvent e => $"Yeniden planlandi: {e.OldDate:dd.MM.yyyy HH:mm} -> {e.NewDate:dd.MM.yyyy HH:mm}",
                AppointmentCompletedEvent e => $"Tamamlandi: {e.AppointmentDate:dd.MM.yyyy HH:mm}",
                AppointmentEmployeeReassignedEvent e => $"Calisan degisti: {e.OldEmployeeId} -> {e.NewEmployeeId}",
                AppointmentStatusChangedEvent e => $"Status: {e.OldStatus} -> {e.NewStatus}",
                _ => domainEvent.GetType().Name
            };
        }

        private static EmailPlan? BuildEmailPlan(IDomainEvent domainEvent)
        {
            static string Page(string title, string body) =>
                $"<html><body><h2>{title}</h2><p>{body}</p></body></html>";

            return domainEvent switch
            {
                AppointmentCreatedEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuz alındı",
                    Page("Randevunuz alındı", $"Randevunuz {e.AppointmentDate:dd.MM.yyyy HH:mm} için oluşturuldu. Onaylandığında tekrar bilgilendirileceksiniz.")),
                AppointmentConfirmedEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuz onaylandı",
                    Page("Randevunuz onaylandı", $"{e.AppointmentDate:dd.MM.yyyy HH:mm} randevunuz onaylandı.")),
                AppointmentCancelledEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuz iptal edildi",
                    Page("Randevunuz iptal edildi", $"{e.AppointmentDate:dd.MM.yyyy HH:mm} randevunuz iptal edildi. {e.Reason}")),
                AppointmentRescheduledEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuz yeniden planlandı",
                    Page("Randevunuz yeniden planlandı", $"Randevunuz {e.OldDate:dd.MM.yyyy HH:mm} tarihinden {e.NewDate:dd.MM.yyyy HH:mm} tarihine alındı.")),
                AppointmentCompletedEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuz tamamlandı",
                    Page("Randevunuz tamamlandı", $"{e.AppointmentDate:dd.MM.yyyy HH:mm} randevunuz tamamlandı. Değerlendirmenizi bekliyoruz.")),
                AppointmentEmployeeReassignedEvent e => new EmailPlan(
                    e.UserId,
                    "Randevunuzun çalışanı değişti",
                    Page("Randevunuzun çalışanı değişti", $"{e.AppointmentDate:dd.MM.yyyy HH:mm} randevunuz için çalışan ataması güncellendi.")),
                _ => null
            };
        }

        private sealed record EmailPlan(Guid UserId, string Subject, string HtmlBody)
        {
            public string? TextBody => null;
        }
    }
}
