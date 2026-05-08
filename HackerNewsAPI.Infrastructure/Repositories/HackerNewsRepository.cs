using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;

namespace HackerNewsAPI.Infrastructure.Repositories;

public class HackerNewsRepository : IHackerNewsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HackerNewsRepository> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public HackerNewsRepository(ApplicationDbContext context, ILogger<HackerNewsRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Story?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving story {StoryId} from database", storyId);
            var item = await _context.Items.FindAsync(new object[] { storyId }, cancellationToken);
            return item != null ? MapToStory(item) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving story {StoryId} from database", storyId);
            throw;
        }
    }

    public async Task AddOrUpdateStoryAsync(Story story, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = MapToItem(story);
            item.CachedAt = DateTime.UtcNow;
            
            var existingItem = await _context.Items.FindAsync(new object[] { item.Id }, cancellationToken);
            
            if (existingItem != null)
            {
                _logger.LogDebug("Updating story {StoryId} in database", story.Id);
                _context.Entry(existingItem).CurrentValues.SetValues(item);
            }
            else
            {
                _logger.LogDebug("Adding new story {StoryId} to database", story.Id);
                _context.Items.Add(item);
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding or updating story {StoryId} in database", story.Id);
            throw;
        }
    }

    public async Task<bool> IsStoryExpiredAsync(int storyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = await _context.Items.FindAsync(new object[] { storyId }, cancellationToken);
            
            if (item == null)
            {
                return true;
            }
            
            var isExpired = DateTime.UtcNow - item.CachedAt > CacheExpiration;
            
            if (isExpired)
            {
                _logger.LogDebug("Story {StoryId} is expired (cached at {CachedAt})", storyId, item.CachedAt);
            }
            
            return isExpired;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if story {StoryId} is expired", storyId);
            throw;
        }
    }

    public async Task<Dictionary<int, bool>> CheckStoriesExpiredAsync(IEnumerable<int> storyIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await _context.Items
                .Where(i => storyIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i, cancellationToken);
            
            return storyIds.ToDictionary(
                id => id, 
                id => !items.ContainsKey(id) || DateTime.UtcNow - items[id].CachedAt > CacheExpiration
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking batch expiration for stories");
            throw;
        }
    }

    private static Story MapToStory(Item item)
    {
        return new Story
        {
            Title = item.Title,
            Uri = item.Url ?? string.Empty,
            PostedBy = item.By,
            Time = DateTimeOffset.FromUnixTimeSeconds(item.Time).UtcDateTime,
            Score = item.Score,
            CommentCount = item.Descendants
        };
    }

    private static Item MapToItem(Story story)
    {
        return new Item
        {
            Id = story.Id,
            Title = story.Title,
            Url = story.Uri,
            By = story.PostedBy,
            Time = new DateTimeOffset(story.Time).ToUnixTimeSeconds(),
            Score = story.Score,
            Descendants = story.CommentCount,
            Type = Domain.Enums.ItemType.Story,
            CachedAt = DateTime.UtcNow
        };
    }
}
