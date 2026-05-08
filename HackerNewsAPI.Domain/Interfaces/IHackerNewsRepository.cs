using HackerNewsAPI.Domain.Entities;

namespace HackerNewsAPI.Domain.Interfaces;

public interface IHackerNewsRepository
{
    Task<Story?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default);
    Task AddOrUpdateStoryAsync(Story story, CancellationToken cancellationToken = default);
    Task<bool> IsStoryExpiredAsync(int storyId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, bool>> CheckStoriesExpiredAsync(IEnumerable<int> storyIds, CancellationToken cancellationToken = default);
}
