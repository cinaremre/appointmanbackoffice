using Rezervise.Application.Common.Interfaces;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Rezervise.Infrastructure.Services
{
    /// <summary>
    /// Yonetici denetim kayitlarini dogrudan repository uzerinden kalici hale
    /// getirir. Yazim hatasi is akisini bozmaz: kayit atlanir ve loglanir.
    /// </summary>
    public class AdminAuditSink : IAdminAuditSink
    {
        private readonly IRepository<AdminAuditLog> _auditRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AdminAuditSink> _logger;

        public AdminAuditSink(
            IRepository<AdminAuditLog> auditRepository,
            IUnitOfWork unitOfWork,
            ILogger<AdminAuditSink> logger)
        {
            _auditRepository = auditRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task RecordAsync(
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
            try
            {
                var entry = new AdminAuditLog(
                    actorUserId,
                    actorRole,
                    businessId,
                    entityType,
                    entityId,
                    action,
                    DateTime.UtcNow,
                    oldValues,
                    newValues,
                    reason,
                    correlationId,
                    ipAddress);

                await _auditRepository.AddAsync(entry, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Denetim yazimi ana islemi bozmamali; kayip loglanir.
                _logger.LogWarning(
                    ex,
                    "AdminAuditLog yazilamadi. Actor={ActorUserId} Entity={EntityType}/{EntityId} Action={Action}",
                    actorUserId,
                    entityType,
                    entityId,
                    action);
            }
        }
    }
}
