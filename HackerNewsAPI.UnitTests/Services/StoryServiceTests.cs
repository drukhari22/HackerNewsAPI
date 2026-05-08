using HackerNewsAPI.Application.Services;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Services;

public class StoryServiceTests
{
    private readonly Mock<IHackerNewsRepository> _mockRepository;
    private readonly Mock<IHackerNewsApiService> _mockApiService;
    private readonly Mock<ILogger<StoryService>> _mockLogger;
    private readonly StoryService _storyService;

    public StoryServiceTests()
    {
        _mockRepository = new Mock<IHackerNewsRepository>();
        _mockApiService = new Mock<IHackerNewsApiService>();
        _mockLogger = new Mock<ILogger<StoryService>>();
        _storyService = new StoryService(_mockRepository.Object, _mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnStoriesOrderedByScoreDescending()
    {
        var storyIds = new[] { 1, 2, 3 };
        var stories = new[]
        {
            new Story { Title = "Story 1", Uri = "http://example.com/1", PostedBy = "user1", Score = 100, CommentCount = 10, Time = DateTime.UtcNow },
            new Story { Title = "Story 2", Uri = "http://example.com/2", PostedBy = "user2", Score = 300, CommentCount = 20, Time = DateTime.UtcNow },
            new Story { Title = "Story 3", Uri = "http://example.com/3", PostedBy = "user3", Score = 200, CommentCount = 15, Time = DateTime.UtcNow }
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories[id - 1]);

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(3, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 2", storyArray[0].Title); // Highest score
        Assert.Equal("Story 3", storyArray[1].Title); // Medium score
        Assert.Equal("Story 1", storyArray[2].Title); // Lowest score
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldReturnRequestedNumberOfStories()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = storyIds.Select(id => new Story 
        { 
            Title = $"Story {id}", 
            Uri = $"http://example.com/{id}",
            PostedBy = $"user{id}",
            Score = id * 100, 
            CommentCount = id * 10,
            Time = DateTime.UtcNow
        }).ToArray();

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories[id - 1]);

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldFilterOutNullStories()
    {
        var storyIds = new[] { 1, 2, 3 };
        var stories = new Story?[]
        {
            new Story { Title = "Story 1", Uri = "http://example.com/1", PostedBy = "user1", Score = 100, CommentCount = 10, Time = DateTime.UtcNow },
            null,
            new Story { Title = "Story 3", Uri = "http://example.com/3", PostedBy = "user3", Score = 200, CommentCount = 15, Time = DateTime.UtcNow }
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories[id - 1]);

        var result = await _storyService.GetBestStoriesAsync(3);

        Assert.Equal(2, result.Count());
        var storyArray = result.ToArray();
        Assert.Equal("Story 3", storyArray[0].Title); // Higher score comes first
        Assert.Equal("Story 1", storyArray[1].Title);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldFetchStoriesInParallel()
    {
        var storyIds = new[] { 1, 2, 3, 4, 5 };
        var stories = storyIds.Select(id => new Story 
        { 
            Title = $"Story {id}", 
            Uri = $"http://example.com/{id}",
            PostedBy = $"user{id}",
            Score = id * 100, 
            CommentCount = id * 10,
            Time = DateTime.UtcNow
        }).ToArray();

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken ct) => stories[id - 1]);

        var result = await _storyService.GetBestStoriesAsync(5);

        Assert.Equal(5, result.Count());
        
        // Verify that GetStoryAsync was called for all story IDs (parallel execution)
        _mockApiService.Verify(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldMapStoryToStoryDtoCorrectly()
    {
        var storyIds = new[] { 1 };
        var story = new Story 
        { 
            Title = "Test Story", 
            Uri = "https://example.com/test",
            PostedBy = "testuser",
            Score = 150, 
            CommentCount = 25,
            Time = new DateTime(2023, 10, 15, 14, 30, 0, DateTimeKind.Utc)
        };

        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(storyIds);

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        var result = await _storyService.GetBestStoriesAsync(1);

        var storyDto = result.First();
        Assert.Equal(story.Title, storyDto.Title);
        Assert.Equal(story.Uri, storyDto.Uri);
        Assert.Equal(story.PostedBy, storyDto.PostedBy);
        Assert.Equal(story.Score, storyDto.Score);
        Assert.Equal(story.CommentCount, storyDto.CommentCount);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ShouldThrowExceptionWhenApiServiceFails()
    {
        _mockApiService.Setup(s => s.GetTopStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API service error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _storyService.GetBestStoriesAsync(5));
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnStoryFromCacheWhenNotExpired()
    {
        var storyId = 123;
        var story = new Story
        {
            Title = "Cached Story",
            Uri = "https://example.com/cached",
            PostedBy = "testuser",
            Score = 100,
            CommentCount = 25,
            Time = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockRepository.Setup(r => r.GetStoryByIdAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.NotNull(result);
        Assert.Equal("Cached Story", result.Title);
        _mockApiService.Verify(s => s.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldFetchFromApiWhenExpired()
    {
        var storyId = 123;
        var story = new Story
        {
            Title = "Fresh Story",
            Uri = "https://example.com/fresh",
            PostedBy = "testuser",
            Score = 150,
            CommentCount = 30,
            Time = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(story);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.NotNull(result);
        Assert.Equal("Fresh Story", result.Title);
        _mockRepository.Verify(r => r.AddOrUpdateStoryAsync(It.IsAny<Story>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnNullWhenStoryNotFound()
    {
        var storyId = 999;

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story?)null);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnNullWhenStoryIsNull()
    {
        var storyId = 123;

        _mockRepository.Setup(r => r.IsStoryExpiredAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockApiService.Setup(s => s.GetStoryAsync(storyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Story?)null);

        var result = await _storyService.GetStoryByIdAsync(storyId);

        Assert.Null(result);
    }
}
