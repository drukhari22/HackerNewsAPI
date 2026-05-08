using HackerNewsAPI.Infrastructure.Repositories;
using HackerNewsAPI.Infrastructure.Data;
using HackerNewsAPI.Infrastructure.Entities;
using HackerNewsAPI.Domain.Entities;
using HackerNewsAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsAPI.Tests.Repositories;

public class HackerNewsRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HackerNewsRepository>> _mockLogger;
    private readonly HackerNewsRepository _repository;

    public HackerNewsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<HackerNewsRepository>>();
        _repository = new HackerNewsRepository(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnStoryWhenExists()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetStoryByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Story", result.Title);
        Assert.Equal("testuser", result.PostedBy);
        Assert.Equal(100, result.Score);
        Assert.Equal(25, result.CommentCount);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Act
        var result = await _repository.GetStoryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddOrUpdateStoryAsync_ShouldAddNewStory()
    {
        // Arrange
        var story = new Story
        {
            Title = "Test Story",
            Uri = "https://example.com/test",
            PostedBy = "testuser",
            Score = 100,
            CommentCount = 25,
            Time = DateTime.UtcNow
        };

        // Act
        await _repository.AddOrUpdateStoryAsync(story);

        // Assert
        var result = await _context.Items.FirstOrDefaultAsync();
        Assert.NotNull(result);
        Assert.Equal("Test Story", result.Title);
    }

    [Fact]
    public async Task AddOrUpdateStoryAsync_ShouldUpdateExistingStory()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var updatedStory = new Story
        {
            Id = 1,
            Title = "Updated Story",
            Uri = "https://example.com/updated",
            PostedBy = "testuser",
            Score = 150,
            CommentCount = 30,
            Time = DateTime.UtcNow
        };

        // Act
        await _repository.AddOrUpdateStoryAsync(updatedStory);

        // Assert
        var result = await _context.Items.FindAsync(1);
        Assert.NotNull(result);
        Assert.Equal("Updated Story", result.Title);
        Assert.Equal(150, result.Score);
    }

    [Fact]
    public async Task IsStoryExpiredAsync_ShouldReturnTrueWhenStoryNotExists()
    {
        // Act
        var result = await _repository.IsStoryExpiredAsync(999);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStoryExpiredAsync_ShouldReturnTrueWhenStoryExpired()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsStoryExpiredAsync(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsStoryExpiredAsync_ShouldReturnFalseWhenStoryNotExpired()
    {
        // Arrange
        var item = new Item
        {
            Id = 1,
            By = "testuser",
            Title = "Test Story",
            Score = 100,
            Descendants = 25,
            Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Url = "https://example.com/test",
            Type = ItemType.Story,
            CachedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsStoryExpiredAsync(1);

        // Assert
        Assert.False(result);
    }
}
