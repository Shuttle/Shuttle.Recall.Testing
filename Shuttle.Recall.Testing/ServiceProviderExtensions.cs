using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shuttle.Contract;

namespace Shuttle.Recall.Testing;

public static class ServiceProviderExtensions
{
    public static ILogger<T> GetLogger<T>(this IServiceProvider serviceProvider)
    {
        return Guard.AgainstNull(serviceProvider).GetService<ILoggerFactory>()?.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    public static ILogger GetLogger(this IServiceProvider serviceProvider)
    {
        return Guard.AgainstNull(serviceProvider).GetService<ILoggerFactory>()?.CreateLogger("Fixture") ?? NullLogger.Instance;
    }

    public static async Task<IServiceProvider> StartHostedServicesAsync(this IServiceProvider serviceProvider)
    {
        var logger = Guard.AgainstNull(serviceProvider).GetLogger();

        logger.LogInformation("[StartHostedServices]");

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            logger.LogInformation($"[HostedService/Starting] : {hostedService.GetType().Name}");

            await hostedService.StartAsync(CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation($"[HostedService/Started] : {hostedService.GetType().Name}");
        }

        return serviceProvider;
    }

    public static async Task<IServiceProvider> StopHostedServicesAsync(this IServiceProvider serviceProvider)
    {
        var logger = Guard.AgainstNull(serviceProvider).GetLogger();

        logger.LogInformation("[StopHostedServices]");

        foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
        {
            logger.LogInformation($"[HostedService/Stopping] : {hostedService.GetType().Name}");

            await hostedService.StopAsync(CancellationToken.None).ConfigureAwait(false);

            logger.LogInformation($"[HostedService/Stopped] : {hostedService.GetType().Name}");
        }

        return serviceProvider;
    }
}