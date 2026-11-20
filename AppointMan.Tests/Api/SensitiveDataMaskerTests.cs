using AppointMan.Api.Logging;

namespace AppointMan.Tests.Api;

public class SensitiveDataMaskerTests
{
    [Fact]
    public void MaskJson_ShouldMaskNestedSensitiveFields()
    {
        const string json = """
            {"userName":"emre","password":"very-secret","nested":{"token":"abc","keep":"visible"}}
            """;

        var masked = SensitiveDataMasker.MaskJson(json);

        Assert.NotNull(masked);
        Assert.DoesNotContain("very-secret", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("abc", masked, StringComparison.Ordinal);
        Assert.Contains("emre", masked, StringComparison.Ordinal);
        Assert.Contains("visible", masked, StringComparison.Ordinal);
        Assert.Contains(SensitiveDataMasker.MaskedValue, masked, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskJson_MalformedBody_ShouldNeverEchoRawText()
    {
        const string malformed = "password=very-secret&token=abc";

        var masked = SensitiveDataMasker.MaskJson(malformed);

        Assert.Equal(SensitiveDataMasker.InvalidJsonPlaceholder, masked);
    }

    [Fact]
    public void MaskAndSerialize_ShouldTruncateWithinByteBudget()
    {
        var payload = new { Description = new string('x', 500) };

        var masked = SensitiveDataMasker.MaskAndSerialize(payload, maxBytes: 64);

        Assert.NotNull(masked);
        Assert.Contains(SensitiveDataMasker.TruncationMarker, masked, StringComparison.Ordinal);
    }
}
