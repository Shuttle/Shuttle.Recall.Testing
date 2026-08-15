# Shuttle.Recall.Testing

Contains `RecallFixture`, a base class of scripted test scenarios for verifying a custom `Shuttle.Recall` storage/event-processing implementation — i.e. your own implementations of `IPrimitiveEventRepository`, `IPrimitiveEventSequencer`, and `IProjectionEventService`.

## Installation

```bash
dotnet add package Shuttle.Recall.Testing
```

This package has a hard dependency on `NUnit` and `Moq`, since `RecallFixture`'s methods make assertions directly using `NUnit.Framework.Assert`.

## Usage

Derive a test class from `RecallFixture` and, in each test, build an `IServiceCollection` that registers your backend's implementations alongside `Shuttle.Recall` itself (via `AddRecall()`), then call the relevant `Exercise*Async` method wrapped in a `RecallFixtureOptions`:

```c#
public class MyStorageFixture : RecallFixture
{
    [Test]
    public async Task Should_be_able_to_exercise_event_processing_async()
    {
        var services = new ServiceCollection()
            .AddSingleton<IPrimitiveEventRepository, MyPrimitiveEventRepository>()
            .AddSingleton<IProjectionEventService, MyProjectionEventService>()
            .AddSingleton<IHostedService, MyFixtureHostedService>();

        await ExerciseEventProcessingAsync(new RecallFixtureOptions(services)
            .WithEventProcessingHandlerTimeout(TimeSpan.FromSeconds(5)));
    }
}
```

`RecallFixture` itself composes the real `IEventStore`/`IEventProcessor` (via `AddRecall()`) on top of the services you register — you never implement `IEventStore`/`IEventProcessor` directly, only the lower-level backend interfaces above.

### Exercise methods

| Method | Verifies |
|--------|----------|
| `ExerciseEventProcessingAsync` | Basic event storage and projection processing |
| `ExerciseEventProcessingVolumeAsync` | Processing under load (does not clear data between runs — do not reuse the same backing store across runs) |
| `ExerciseEventProcessingWithDeferredHandlingAsync` | A projection handler calling `context.Defer(...)` |
| `ExerciseEventProcessingWithDelayAsync` | Processing behavior when handling is delayed |
| `ExerciseEventProcessingWithFailureAsync` | Behavior when a projection handler throws |
| `ExerciseImmediateConsistencyAsync` | Immediate-consistency handling on `SaveAsync` |
| `ExercisePrimitiveEventSequencerAsync` | `IPrimitiveEventSequencer` behavior |
| `ExerciseStorageAsync` | Basic `IEventStore` save/retrieve/remove behavior |

### RecallFixtureOptions

| Member | Description |
|--------|-------------|
| `Services` | The `IServiceCollection` supplied to the constructor |
| `WithStarting(Func<IServiceProvider, Task>)` | Callback invoked once services are built, before the scenario runs |
| `WithEventProcessingHandlerTimeout(TimeSpan)` | How long to wait for a projection handler to run (default `5s`) |
| `WithPrimitiveEventSequencerTimeout(TimeSpan)` | How long to wait for the primitive event sequencer (default `5s`) |
| `WithEventStreamTask(Func<IServiceProvider, Func<Task>, Task>)` | Customizes how the fixture's event-stream-producing task is run |
| `WithItemAdded(Func<IEventHandlerContext<ItemAdded>, Task>)` | Hook invoked when the fixture's built-in `ItemAdded` projection handler runs |

## Shuttle.Recall.Testing.Memory

`Shuttle.Recall.Testing.Memory` (in this repository) is **not** a published package — it's a worked example, an NUnit test project that implements a minimal in-memory backend (`IPrimitiveEventRepository`, `IPrimitiveEventSequencer`, `IProjectionEventService`) and runs every `RecallFixture` scenario against it. Use it as a reference for wiring up your own backend's fixture tests.
