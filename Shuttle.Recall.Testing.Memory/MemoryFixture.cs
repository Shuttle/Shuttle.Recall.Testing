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
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingAsync(new RecallFixtureOptions(services)
            .WithEventProcessingHandlerTimeout(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_volume()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingVolumeAsync(new RecallFixtureOptions(services)
            .WithEventProcessingHandlerTimeout(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_with_delay_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithDelayAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_with_failure_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithFailureAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_processing_with_deferred_handling_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithDeferredHandlingAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_event_store_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddRecall()
            .Services;

        await ExerciseStorageAsync(new(services));
    }

    [Test]
    public async Task Should_be_able_to_exercise_sequencer_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IPrimitiveEventSequencer, MemoryPrimitiveEventSequencer>()
            .AddRecall(options =>
            {
                options.EventStore.PrimitiveEventSequencerIdleDurations = [TimeSpan.FromMilliseconds(25)];
            })
            .Services;

        await ExercisePrimitiveEventSequencerAsync(new(services));
    }
}