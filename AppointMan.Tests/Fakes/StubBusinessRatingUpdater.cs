using AppointMan.Application.Common.Abstractions;

namespace AppointMan.Tests.Fakes;

public sealed class StubBusinessRatingUpdater : IBusinessRatingUpdater
{
    public List<(Guid BusinessId, int Score)> Added { get; } = new();
    public List<(Guid BusinessId, int Score)> Removed { get; } = new();

    public Task<int> UpdateOnNewReviewAsync(
        Guid businessId,
        int score,
        CancellationToken cancellationToken = default)
    {
        Added.Add((businessId, score));
        return Task.FromResult(1);
    }

    public Task<int> UpdateOnReviewRemovedAsync(
        Guid businessId,
        int score,
        CancellationToken cancellationToken = default)
    {
        Removed.Add((businessId, score));
        return Task.FromResult(1);
    }
}
