using AppointMan.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppointMan.Infrastructure.Services
{
    /// <summary>
    /// İş akışı denetim kayıtlarını yapılandırılmış log kanalına yazar.
    /// GELF/Graylog sağlayıcısı bu alanları ayrı alanlar olarak taşır.
    /// </summary>
    public sealed class LoggingWorkflowAuditSink : IWorkflowAuditSink
    {
        private readonly ILogger<LoggingWorkflowAuditSink> _logger;

        public LoggingWorkflowAuditSink(ILogger<LoggingWorkflowAuditSink> logger)
        {
            _logger = logger;
        }

        public void Record(string eventType, Guid eventId, Guid? aggregateId, string? details)
        {
            _logger.LogInformation(
                "WorkflowAudit EventType={EventType} EventId={EventId} AggregateId={AggregateId} Details={Details}",
                eventType,
                eventId,
                aggregateId,
                details);
        }
    }
}
