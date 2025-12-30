using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Recall.Testing.Memory.Fakes;

/// <summary>
///     This is a naive implementation of a projection service in that we only have the 'recall-fixture' projection to worry about.
/// </summary>
public class MemoryProjectionService(IOptions<RecallOptions> recallOptions, IPrimitiveEventStore primitiveEventStore, IEventProcessorConfiguration eventProcessorConfiguration)
    : IProjectionService, IPipelineObserver<ThreadPoolsStarted>
{
    private readonly RecallOptions _recallOptions = Guard.AgainstNull(Guard.AgainstNull(recallOptions).Value);

    private class BalancedProjection(Projection projection, IEnumerable<TimeSpan> backoffDurations)
    {
        private readonly TimeSpan[] _durations = Guard.AgainstEmpty(backoffDurations).ToArray();
        private int _durationIndex;

        public Projection Projection { get; } = projection;
        public DateTimeOffset BackoffTillDate { get; private set; } = DateTimeOffset.MinValue;
        
        public void Backoff()
        {
            if (_durationIndex >= _durations.Length)
            {
                _durationIndex = _durations.Length - 1;
            }

            BackoffTillDate = DateTimeOffset.UtcNow.Add(_durations[_durationIndex++]);
        }

        public void Resume()
        {
            BackoffTillDate = DateTimeOffset.MinValue;
            _durationIndex = 0;
        }
    }

    private readonly IEventProcessorConfiguration _eventProcessorConfiguration = Guard.AgainstNull(eventProcessorConfiguration);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IPrimitiveEventStore _primitiveEventStore = Guard.AgainstNull(primitiveEventStore);
    private readonly Dictionary<string, List<ThreadPrimitiveEvent>> _projectionThreadPrimitiveEvents = new();
    private int[] _managedThreadIds = [];

    private BalancedProjection[] _projections = [];
    private int _roundRobinIndex;

    public async Task<ProjectionEvent?> RetrieveEventAsync(IPipelineContext<RetrieveEvent> pipelineContext, CancellationToken cancellationToken = default)
    {
        var processorThreadManagedThreadId = Guard.AgainstNull(pipelineContext).Pipeline.State.GetProcessorThreadManagedThreadId();

        if (_projections.Length == 0)
        {
            return null;
        }

        for (var i = 0; i < _projections.Length; i++)
        {
            BalancedProjection balancedProjection;

            await _lock.WaitAsync(cancellationToken);
            try
            {
                var currentIndex = (_roundRobinIndex + i) % _projections.Length;
                balancedProjection = _projections[currentIndex];
                _roundRobinIndex = (currentIndex + 1) % _projections.Length;
            }
            finally
            {
                _lock.Release();
            }

            if (balancedProjection.BackoffTillDate > DateTimeOffset.UtcNow)
            {
                continue;
            }

            var projectionThreadPrimitiveEvents = _projectionThreadPrimitiveEvents[balancedProjection.Projection.Name];

            if (!projectionThreadPrimitiveEvents.Any())
            {
                await GetProjectionJournalAsync(balancedProjection.Projection);
            }

            if (!projectionThreadPrimitiveEvents.Any())
            {
                balancedProjection.Backoff();
                continue;
            }

            balancedProjection.Resume();

            var threadPrimitiveEvent = projectionThreadPrimitiveEvents.FirstOrDefault(item => item.ManagedThreadId == processorThreadManagedThreadId);

            if (threadPrimitiveEvent != null)
            {
                return new(balancedProjection.Projection, threadPrimitiveEvent.PrimitiveEvent);
            }
        }

        return null;
    }

    public async Task AcknowledgeEventAsync(IPipelineContext<AcknowledgeEvent> pipelineContext, CancellationToken cancellationToken = default)
    {
        var projectionEvent = Guard.AgainstNull(pipelineContext).Pipeline.State.GetProjectionEvent();

        await _lock.WaitAsync(cancellationToken);

        try
        {
            _projectionThreadPrimitiveEvents[projectionEvent.Projection.Name].RemoveAll(item => item.PrimitiveEvent.SequenceNumber == projectionEvent.PrimitiveEvent.SequenceNumber);
        }
        finally
        {
            _lock.Release();
        }

        projectionEvent.Projection.Commit(projectionEvent.PrimitiveEvent.SequenceNumber!.Value);

        await Task.CompletedTask;
    }

    private async Task GetProjectionJournalAsync(Projection projection)
    {
        // This would get the next batch of primitive event details for the service te return.
        // Include only event types that are handled in this process (IProjectionConfiguration).
        // Once entire batch has completed, update the sequence number for the projections.
        // This is a naive implementation and should be replaced with a more efficient one in actual implementations.

        await _lock.WaitAsync();

        try
        {
            if (_projectionThreadPrimitiveEvents[projection.Name].Any())
            {
                return;
            }

            foreach (var primitiveEvent in (await _primitiveEventStore.GetCommittedPrimitiveEventsAsync(projection.SequenceNumber + 1)).OrderBy(item => item.SequenceNumber))
            {
                var managedThreadId = _managedThreadIds[Math.Abs((primitiveEvent.CorrelationId ?? primitiveEvent.Id).GetHashCode()) % _managedThreadIds.Length];

                _projectionThreadPrimitiveEvents[projection.Name].Add(new(managedThreadId, primitiveEvent));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExecuteAsync(IPipelineContext<ThreadPoolsStarted> pipelineContext, CancellationToken cancellationToken = default)
    {
        var processorThreadPool = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.Get<IProcessorThreadPool>("ProjectionProcessorThreadPool"));

        List<BalancedProjection> balancedProjections = [];

        await _lock.WaitAsync(cancellationToken);

        try
        {
            foreach (var projectionConfiguration in _eventProcessorConfiguration.Projections)
            {
                balancedProjections.Add(new(new(projectionConfiguration.Name, 0), _recallOptions.EventProcessing.ProjectionProcessorIdleDurations));
                _projectionThreadPrimitiveEvents.Add(projectionConfiguration.Name, []);
            }

            _projections = balancedProjections.ToArray();
            _managedThreadIds = processorThreadPool.ProcessorThreads.Select(item => item.ManagedThreadId).ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }
}