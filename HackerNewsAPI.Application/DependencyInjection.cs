using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using HackerNewsAPI.Application.Interfaces;
using HackerNewsAPI.Application.Services;

namespace HackerNewsAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register application services
        services.AddScoped<IStoryService, StoryService>();
        services.AddScoped<IAuthService, AuthService>();

        // Register HttpClient for Hacker News API service
        services.AddHttpClient<IHackerNewsApiService, HackerNewsApiService>(client =>
        {
            client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHostedService<CacheRefreshService>();

        return services;
    }
}
