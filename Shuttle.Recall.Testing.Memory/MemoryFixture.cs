using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Shuttle.Recall.Testing.Memory.Fakes;

namespace Shuttle.Recall.Testing.Memory;

public class MemoryFixture : RecallFixture
{
    [Test]
    public async Task Should_be_able_to_exercise_event_processing_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IProjectionService, MemoryProjectionService>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryFixtureStartupObserver>();

        await ExerciseEventProcessingAsync(new FixtureConfiguration(services)
            .WithHandlerTimeout(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_volume()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IProjectionService, MemoryProjectionService>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryFixtureStartupObserver>();

        await ExerciseEventProcessingVolumeAsync(new FixtureConfiguration(services)
            .WithAddEventStore(builder =>
            {
                builder.Options.ProjectionThreadCount = 25;
            })
            .WithHandlerTimeout(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_with_delay_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IProjectionService, MemoryProjectionService>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryFixtureStartupObserver>();

        await ExerciseEventProcessingWithDelayAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_with_failure_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IProjectionService, MemoryProjectionService>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryFixtureStartupObserver>();

        await ExerciseEventProcessingWithFailureAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_store_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>();

        await ExerciseStorageAsync(new(services));
    }
}