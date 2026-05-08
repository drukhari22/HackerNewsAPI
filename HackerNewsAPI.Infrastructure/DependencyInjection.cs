using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using HackerNewsAPI.Domain.Interfaces;
using HackerNewsAPI.Infrastructure.Repositories;
using HackerNewsAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HackerNewsAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register memory cache
        services.AddMemoryCache();
        
        // Register DbContext with InMemory database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase("HackerNewsDb");
        });

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHackerNewsRepository, HackerNewsRepository>();

        // Create a scope to seed the database
        var serviceProvider = services.BuildServiceProvider();
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.EnsureCreated();
        }

        return services;
    }
}
