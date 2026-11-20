using System.Globalization;
using AppointMan.Application.Common.Interfaces;

namespace AppointMan.Tests.Fakes;

public sealed class StubLocalizationService : ILocalizationService
{
    public CultureInfo CurrentCulture { get; set; } = new("tr-TR");

    public string Get(string key) => key;
    public string Get(string key, params object[] args) => key;
    public string Get(string key, CultureInfo culture) => key;
    public bool HasKey(string key, string cultureName) => false;
}
