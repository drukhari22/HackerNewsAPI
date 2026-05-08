using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace HackerNewsAPI.Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        // Domain layer typically has minimal DI registrations
        // Repository interfaces are defined here but implementations are registered in Infrastructure
        return services;
    }
}
