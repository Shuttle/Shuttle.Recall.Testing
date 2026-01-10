using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines.Logging;
using Shuttle.Recall.Logging;

namespace Shuttle.Recall.Testing;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureLogging(string test)
        {
            Guard.AgainstNull(services);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new FixtureFileLoggerProvider(Guard.AgainstEmpty(test))));
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleLoggerProvider>());

            services
                .AddPipelineLogging()
                .AddRecallLogging()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                });

            return services;
        }
    }
}