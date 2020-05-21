
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordBotServer.Utilities
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddSingletonHostedService<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddSingleton<THostedService>();
            services.AddHostedService(provider => provider.GetRequiredService<THostedService>());
            return services;
        }
    }
}
