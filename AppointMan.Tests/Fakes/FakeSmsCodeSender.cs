using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Tests.Fakes;

/// <summary>
/// Gonderilen SMS kodlarini bellekte tutan test cifti. Gercek saglayici
/// baglanana kadar OTP akisinin dogrulanabilmesi icin kullanilir.
/// </summary>
public sealed class FakeSmsCodeSender : ISmsCodeSender
{
    private readonly List<(string PhoneNumber, string Code)> _sent = new();

    /// <summary>Gonderilen (numara, kod) ciftleri.</summary>
    public IReadOnlyList<(string PhoneNumber, string Code)> Sent => _sent;

    /// <summary>Toplam gonderim sayisi.</summary>
    public int SendCount => _sent.Count;

    public Task SendAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sent.Add((phoneNumber, code));

        return Task.CompletedTask;
    }
}
