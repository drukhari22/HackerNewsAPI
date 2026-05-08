using HackerNewsAPI.Domain.Entities;

namespace HackerNewsAPI.Application.Interfaces;

public interface IHackerNewsApiService
{
    Task<Story?> GetStoryAsync(int storyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<int>> GetTopStoryIdsAsync(CancellationToken cancellationToken = default);
}
