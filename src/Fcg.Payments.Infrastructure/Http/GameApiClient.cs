using System.Net.Http.Json;
using Fcg.Payments.Application.Services;
using Microsoft.Extensions.Options;

namespace Fcg.Payments.Infrastructure.Http;

public class GameApiClient : IGameApiClient
{
    private readonly HttpClient _http;
    private readonly GamesApiOptions _options;

    public GameApiClient(HttpClient http, IOptions<GamesApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<GameInfo?> GetGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _http.GetAsync($"games/{gameId}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var dto = await response.Content.ReadFromJsonAsync<GameDto>(cancellationToken).ConfigureAwait(false);
            return dto is null ? null : new GameInfo(dto.Id, dto.Title ?? "", dto.Price, dto.IsPublished);
        }
        catch
        {
            return null;
        }
    }

    public async Task AddToLibraryAsync(Guid userId, Guid gameId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.InternalApiKey))
            return;
        var body = new AddFromPaymentBody { UserId = userId, GameId = gameId };
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/library/add-from-payment");
        request.Headers.TryAddWithoutValidation("X-Api-Key", _options.InternalApiKey);
        request.Content = JsonContent.Create(body);
        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private sealed class GameDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
    }

    private sealed class AddFromPaymentBody
    {
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
    }
}

public class GamesApiOptions
{
    public const string SectionName = "GamesApi";
    public string BaseUrl { get; set; } = "";
    public string InternalApiKey { get; set; } = "";
}
