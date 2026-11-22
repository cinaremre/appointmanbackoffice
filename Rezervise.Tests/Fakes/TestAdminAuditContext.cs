using Rezervise.Application.Common.Interfaces;

namespace Rezervise.Tests.Fakes;

/// <summary>
/// Unit testlerde kullanilan sabit denetim baglami.
/// Correlation ve IP degerleri handler kayitlarinda gorunur.
/// </summary>
public sealed class TestAdminAuditContext : IAdminAuditContext
{
    public string CorrelationId { get; set; } = "test-correlation";

    public string IpAddress { get; set; } = "127.0.0.1";
}
