using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Recall.Testing.Order;

namespace Shuttle.Recall.Testing;

public class FixtureConfiguration(IServiceCollection services)
{
    public IServiceCollection Services { get; } = Guard.AgainstNull(services);
    public Action<EventStoreBuilder>? AddEventStore { get; private set; }
    public Func<IServiceProvider, Task>? StartingAsync { get; private set; }
    public TimeSpan EventProcessingHandlerTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public TimeSpan PrimitiveEventSequencerTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public Func<IServiceProvider, Func<Task>, Task>? EventStreamTaskAsync { get; set; }
    public Func<IEventHandlerContext<ItemAdded>, Task>? ItemAddedAsync { get; set; }
    public int VolumeIterationCount { get; set; } = 100;

    public FixtureConfiguration WithAddEventStore(Action<EventStoreBuilder> addEventStore)
    {
        AddEventStore = Guard.AgainstNull(addEventStore);
        return this;
    }

    public FixtureConfiguration WithStarting(Func<IServiceProvider, Task> starting)
    {
        StartingAsync = Guard.AgainstNull(starting);
        return this;
    }

    public FixtureConfiguration WithEventProcessingHandlerTimeout(TimeSpan eventProcessingHandlerTimeout)
    {
        EventProcessingHandlerTimeout = eventProcessingHandlerTimeout;
        return this;
    }

    public FixtureConfiguration WithPrimitiveEventSequencerTimeout(TimeSpan primitiveEventSequencerTimeout)
    {
        PrimitiveEventSequencerTimeout = primitiveEventSequencerTimeout;
        return this;
    }

    public FixtureConfiguration WithEventStreamTask(Func<IServiceProvider, Func<Task>, Task> eventStreamTask)
    {
        EventStreamTaskAsync = Guard.AgainstNull(eventStreamTask);
        return this;
    }

    public FixtureConfiguration WithItemAdded(Func<IEventHandlerContext<ItemAdded>, Task> itemAdded)
    {
        ItemAddedAsync = Guard.AgainstNull(itemAdded);
        return this;
    }
}