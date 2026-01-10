using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shuttle.Core.Contract;
using Shuttle.Recall.Testing.Order;

namespace Shuttle.Recall.Testing;

public class RecallFixtureOptions(IServiceCollection services)
{
    public IServiceCollection Services { get; } = Guard.AgainstNull(services);
    public Action<RecallBuilder>? AddRecall { get; private set; }
    public Func<IServiceProvider, Task>? StartingAsync { get; private set; }
    public TimeSpan EventProcessingHandlerTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public TimeSpan PrimitiveEventSequencerTimeout { get; private set; } = TimeSpan.FromSeconds(5);
    public Func<IServiceProvider, Func<Task>, Task>? EventStreamTaskAsync { get; set; }
    public Func<IEventHandlerContext<ItemAdded>, Task>? ItemAddedAsync { get; set; }
    public int VolumeIterationCount { get; set; } = 100;

    public RecallFixtureOptions WithAddRecall(Action<RecallBuilder> action)
    {
        AddRecall = Guard.AgainstNull(action);
        return this;
    }

    public RecallFixtureOptions WithStarting(Func<IServiceProvider, Task> starting)
    {
        StartingAsync = Guard.AgainstNull(starting);
        return this;
    }

    public RecallFixtureOptions WithEventProcessingHandlerTimeout(TimeSpan eventProcessingHandlerTimeout)
    {
        EventProcessingHandlerTimeout = eventProcessingHandlerTimeout;
        return this;
    }

    public RecallFixtureOptions WithPrimitiveEventSequencerTimeout(TimeSpan primitiveEventSequencerTimeout)
    {
        PrimitiveEventSequencerTimeout = primitiveEventSequencerTimeout;
        return this;
    }

    public RecallFixtureOptions WithEventStreamTask(Func<IServiceProvider, Func<Task>, Task> eventStreamTask)
    {
        EventStreamTaskAsync = Guard.AgainstNull(eventStreamTask);
        return this;
    }

    public RecallFixtureOptions WithItemAdded(Func<IEventHandlerContext<ItemAdded>, Task> itemAdded)
    {
        ItemAddedAsync = Guard.AgainstNull(itemAdded);
        return this;
    }
}