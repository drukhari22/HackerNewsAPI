using Microsoft.Extensions.Logging;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;

namespace HackerNewsAPI.Application.Services;

public class StoryService : IStoryService
{
    private readonly IHackerNewsRepository _hackerNewsRepository;
    private readonly IHackerNewsApiService _hackerNewsApiService;
    private readonly ILogger<StoryService> _logger;

    public StoryService(
        IHackerNewsRepository hackerNewsRepository,
        IHackerNewsApiService hackerNewsApiService,
        ILogger<StoryService> logger)
    {
        _hackerNewsRepository = hackerNewsRepository ?? throw new ArgumentNullException(nameof(hackerNewsRepository));
        _hackerNewsApiService = hackerNewsApiService ?? throw new ArgumentNullException(nameof(hackerNewsApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Story?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting story {StoryId}", storyId);

        try
        {
            var isExpired = await _hackerNewsRepository.IsStoryExpiredAsync(storyId, cancellationToken);
            Story? story;

            if (isExpired)
            {
                _logger.LogDebug("Story {StoryId} is expired or not in cache, fetching from API", storyId);
                story = await _hackerNewsApiService.GetStoryAsync(storyId, cancellationToken);
                
                if (story != null)
                {
                    await _hackerNewsRepository.AddOrUpdateStoryAsync(story, cancellationToken);
                }
            }
            else
            {
                _logger.LogDebug("Story {StoryId} is in cache and not expired", storyId);
                story = await _hackerNewsRepository.GetStoryByIdAsync(storyId, cancellationToken);
            }

            if (story == null)
            {
                _logger.LogWarning("Story {StoryId} not found", storyId);
                return null;
            }

            return story;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting story {StoryId}", storyId);
            throw;
        }
    }

    public async Task<IEnumerable<StoryDto>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting top {Count} best stories", count);

        try
        {
            var storyIds = await _hackerNewsApiService.GetTopStoryIdsAsync(cancellationToken);
            
            // Fetch stories sequentially for now to avoid DbContext concurrency issues
            // TODO: Re-enable parallel processing after fixing repository thread safety
            var stories = new List<Story>();
            foreach (var storyId in storyIds.Take(count))
            {
                var story = await GetStoryByIdAsync(storyId, cancellationToken);
                if (story != null)
                {
                    stories.Add(story);
                }
            }
            
            var validStories = stories.OrderByDescending(s => s.Score);
            
            var storyDtos = validStories.Select(story => new StoryDto
            {
                Title = story.Title,
                Uri = story.Uri,
                PostedBy = story.PostedBy,
                Time = story.Time,
                Score = story.Score,
                CommentCount = story.CommentCount
            });

            _logger.LogInformation("Successfully retrieved {Count} stories", storyDtos.Count());
            return storyDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting best stories");
            throw;
        }
    }
}
