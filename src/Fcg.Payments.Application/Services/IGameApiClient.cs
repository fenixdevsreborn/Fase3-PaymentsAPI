namespace Fcg.Payments.Application.Services;

public interface IGameApiClient
{
    Task<GameInfo?> GetGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task AddToLibraryAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default);
}

public record GameInfo(Guid Id, string Title, decimal Price, bool IsPublished);
