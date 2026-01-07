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
            .AddSingleton<MemoryProjectionService>()
            .AddSingleton<IProjectionService>(sp => sp.GetRequiredService<MemoryProjectionService>());

        await ExerciseEventProcessingAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.SuppressPrimitiveEventSequencerHostedService();
            })
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
            .AddSingleton<MemoryProjectionService>()
            .AddSingleton<IProjectionService>(sp => sp.GetRequiredService<MemoryProjectionService>());

        await ExerciseEventProcessingVolumeAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.SuppressPrimitiveEventSequencerHostedService();
            })
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
            .AddSingleton<MemoryProjectionService>()
            .AddSingleton<IProjectionService>(sp => sp.GetRequiredService<MemoryProjectionService>());

        await ExerciseEventProcessingWithDelayAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.SuppressPrimitiveEventSequencerHostedService();
            }), isTransactional);
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
            .AddSingleton<MemoryProjectionService>()
            .AddSingleton<IProjectionService>(sp => sp.GetRequiredService<MemoryProjectionService>());

        await ExerciseEventProcessingWithFailureAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.SuppressPrimitiveEventSequencerHostedService();
                builder.SuppressEventProcessorHostedService();
            }), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_event_store_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>();

        await ExerciseStorageAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.SuppressPrimitiveEventSequencerHostedService();
            }), isTransactional);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Should_be_able_to_exercise_sequencer_async(bool isTransactional)
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventStore>(new PrimitiveEventStore())
            .AddSingleton<IPrimitiveEventRepository, MemoryPrimitiveEventRepository>()
            .AddSingleton<IPrimitiveEventSequencer, MemoryPrimitiveEventSequencer>();

        await ExercisePrimitiveEventSequencerAsync(new FixtureConfiguration(services)
            .WithAddRecall(builder =>
            {
                builder.Options.EventStore.PrimitiveEventSequencerIdleDurations = [TimeSpan.FromMilliseconds(25)];
            }), isTransactional);
    }
}