using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing;

public static class ServiceProviderExtensions
{
    public static ILogger<T> GetLogger<T>(this IServiceProvider serviceProvider)
    {
        return Guard.AgainstNull(serviceProvider).GetRequiredService<ILoggerFactory>().CreateLogger<T>();
    }

    public static ILogger GetLogger(this IServiceProvider serviceProvider)
    {
        return Guard.AgainstNull(serviceProvider).GetRequiredService<ILoggerFactory>().CreateLogger("Fixture");
    }

    public static async Task<IServiceProvider> StartHostedServicesAsync(this IServiceProvider serviceProvider)
    {
        var logger = Guard.AgainstNull(serviceProvider).GetLogger();

        logger.LogInformation("[StartHostedServices]");

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            logger.LogInformation($"[HostedService-starting] : {hostedService.GetType().Name}");

            await hostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation($"[HostedService-started] : {hostedService.GetType().Name}");
        }

        return serviceProvider;
    }

    public static async Task<IServiceProvider> StopHostedServicesAsync(this IServiceProvider serviceProvider)
    {
        var logger = Guard.AgainstNull(serviceProvider).GetLogger();

        logger.LogInformation("[StopHostedServices]");

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            logger.LogInformation($"[HostedService-stopping] : {hostedService.GetType().Name}");

            await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation($"[HostedService-stopped] : {hostedService.GetType().Name}");
        }

        return serviceProvider;
    }
}