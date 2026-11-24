using Rezervise.Application.Common.Interfaces;
using Rezervise.Application.Common.Models;

namespace Rezervise.Tests.Fakes;

/// <summary>
/// Kuyruğa verilen e-postaları bellekte toplar; SMTP'ye dokunmaz.
/// </summary>
public sealed class FakeEmailQueue : IEmailQueue
{
    public List<EmailMessage> Sent { get; } = new();

    public bool TryEnqueue(EmailMessage message)
    {
        Sent.Add(message);
        return true;
    }
}
