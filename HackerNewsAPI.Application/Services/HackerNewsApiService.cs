using Microsoft.Extensions.Logging;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HackerNewsAPI.Application.Services;

public class HackerNewsApiService : IHackerNewsApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsApiService> _logger;
    private readonly SemaphoreSlim _apiSemaphore;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
    };

    public HackerNewsApiService(HttpClient httpClient, ILogger<HackerNewsApiService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiSemaphore = new SemaphoreSlim(5, 5); // Max 5 concurrent API requests
    }

    public async Task<Story?> GetStoryAsync(int storyId, CancellationToken cancellationToken = default)
    {
        await _apiSemaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Fetching story {StoryId} from Hacker News API", storyId);
            var response = await _httpClient.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{storyId}.json", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch story {StoryId}: {StatusCode}", storyId, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiItem = JsonSerializer.Deserialize<HackerNewsApiItem>(json, JsonOptions);

            if (apiItem != null)
            {
                _logger.LogDebug("Successfully fetched story {StoryId}", storyId);
            }

            return apiItem != null ? MapToStory(apiItem) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching story {StoryId} from Hacker News API", storyId);
            throw;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<IEnumerable<int>> GetTopStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching top story IDs from Hacker News API");
            var response = await _httpClient.GetAsync("https://hacker-news.firebaseio.com/v0/topstories.json?print=pretty", cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var storyIds = JsonSerializer.Deserialize<IEnumerable<int>>(json) ?? Enumerable.Empty<int>();

            _logger.LogDebug("Successfully fetched {Count} story IDs", storyIds.Count());

            return storyIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top story IDs from Hacker News API");
            throw;
        }
    }

    private static Story MapToStory(HackerNewsApiItem apiItem)
    {
        return new Story
        {
            Title = apiItem.Title ?? string.Empty,
            Uri = apiItem.Url ?? string.Empty,
            PostedBy = apiItem.By ?? string.Empty,
            Time = DateTimeOffset.FromUnixTimeSeconds(apiItem.Time).UtcDateTime,
            Score = apiItem.Score,
            CommentCount = apiItem.Descendants
        };
    }

    // DTO for deserializing Hacker News API response
    private class HackerNewsApiItem
    {
        public int Id { get; set; }
        public string? By { get; set; }
        public int Descendants { get; set; }
        public List<int>? Kids { get; set; }
        public int Score { get; set; }
        public long Time { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public ItemType Type { get; set; }
    }
}
