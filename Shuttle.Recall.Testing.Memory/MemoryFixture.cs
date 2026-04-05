using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Shuttle.Recall.Testing.Memory.Fakes;

namespace Shuttle.Recall.Testing.Memory;

public class MemoryFixture : RecallFixture
{
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_processing_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingAsync(new RecallFixtureOptions(services)
            .WithEventProcessingHandlerTimeout(TimeSpan.FromSeconds(5)), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_processing_volume(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingVolumeAsync(new RecallFixtureOptions(services)
            .WithEventProcessingHandlerTimeout(TimeSpan.FromMinutes(5)), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_processing_with_delay_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithDelayAsync(new(services), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_processing_with_failure_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithFailureAsync(new(services), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_processing_with_deferred_handling_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IHostedService, MemoryFixtureHostedService>()
            .AddSingleton<MemoryProjectionEventService>()
            .AddSingleton<IProjectionEventService>(sp => sp.GetRequiredService<MemoryProjectionEventService>());

        await ExerciseEventProcessingWithDeferredHandlingAsync(new(services), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_store_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddRecall()
            .Services;

        await ExerciseStorageAsync(new(services), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_sequencer_async(bool isTransactional)
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

        await ExercisePrimitiveEventSequencerAsync(new(services), isTransactional);
    }
}