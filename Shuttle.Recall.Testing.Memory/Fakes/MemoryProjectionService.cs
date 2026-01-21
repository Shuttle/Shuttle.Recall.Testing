using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;

namespace Shuttle.Recall.Testing.Memory.Fakes;

/// <summary>
///     This is a naive implementation of a projection service in that we only have the 'recall-fixture' projection to
///     worry about.
/// </summary>
public class MemoryProjectionEventService(ILogger<MemoryProjectionEventService> logger, IOptions<RecallOptions> recallOptions, IPrimitiveEventStore primitiveEventStore, IEventProcessorConfiguration eventProcessorConfiguration)
    : IProjectionEventService, IPipelineObserver<ThreadPoolsStarted>
{
    private readonly List<ProjectionExecutionContext> _projectionExecutionContexts = [];
    private readonly IEventProcessorConfiguration _eventProcessorConfiguration = Guard.AgainstNull(eventProcessorConfiguration);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly IPrimitiveEventStore _primitiveEventStore = Guard.AgainstNull(primitiveEventStore);
    private readonly RecallOptions _recallOptions = Guard.AgainstNull(Guard.AgainstNull(recallOptions).Value);
    private int[] _managedThreadIds = [];
    private int _roundRobinIndex;

    public async Task ExecuteAsync(IPipelineContext<ThreadPoolsStarted> pipelineContext, CancellationToken cancellationToken = default)
    {
        var processorThreadPool = Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.Get<IProcessorThreadPool>("ProjectionProcessorThreadPool"));

        await _lock.WaitAsync(cancellationToken);

        try
        {
            foreach (var projectionConfiguration in _eventProcessorConfiguration.Projections)
            {
                _projectionExecutionContexts.Add(new(new(projectionConfiguration.Name, 0), _recallOptions.EventProcessing.ProjectionProcessorIdleDurations));
            }

            _managedThreadIds = processorThreadPool.ProcessorThreads.Select(item => item.ManagedThreadId).ToArray();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ProjectionEvent?> RetrieveAsync(IPipelineContext<RetrieveEvent> pipelineContext, CancellationToken cancellationToken = default)
    {
        var processorThreadManagedThreadId = Guard.AgainstNull(pipelineContext).Pipeline.State.GetProcessorThreadManagedThreadId();

        if (_projectionExecutionContexts.Count == 0)
        {
            return null;
        }

        for (var i = 0; i < _projectionExecutionContexts.Count; i++)
        {
            await _lock.WaitAsync(cancellationToken);

            ProjectionExecutionContext projectionExecutionContext;

            try
            {
                var currentIndex = (_roundRobinIndex + i) % _projectionExecutionContexts.Count;
                projectionExecutionContext = _projectionExecutionContexts[currentIndex];

                if (projectionExecutionContext.IsBackingOff())
                {
                    continue;
                }
            }
            finally
            {
                _lock.Release();
            }

            await projectionExecutionContext.Lock.WaitAsync(cancellationToken);

            try
            {
                if (projectionExecutionContext.IsEmpty)
                {
                    await GetProjectionJournalAsync(projectionExecutionContext);
                }

                if (projectionExecutionContext.IsEmpty)
                {
                    projectionExecutionContext.Backoff();
                    continue;
                }

                projectionExecutionContext.Resume();

                var primitiveEvent = projectionExecutionContext.RetrievePrimitiveEvent(processorThreadManagedThreadId);

                if (primitiveEvent != null)
                {
                    return new(projectionExecutionContext.Projection, primitiveEvent);
                }
            }
            finally
            {
                projectionExecutionContext.Lock.Release();
            }
        }

        _roundRobinIndex = (_roundRobinIndex + 1) % _projectionExecutionContexts.Count;

        return null;
    }

    public async Task DeferAsync(IPipelineContext<HandleEvent> pipelineContext, CancellationToken cancellationToken = default)
    {
        var projectionEvent = Guard.AgainstNull(pipelineContext).Pipeline.State.GetProjectionEvent();
        var deferredUntil = Guard.AgainstNull(pipelineContext).Pipeline.State.GetDeferredUntil();

        if (!deferredUntil.HasValue)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);

        try
        {
            _projectionExecutionContexts.First(item => item.Projection.Name == projectionEvent.Projection.Name).Backoff(deferredUntil);
        }
        finally
        {
            _lock.Release();
        }

    }

    public Task PipelineFailedAsync(IPipelineContext<PipelineFailed> pipelineContext, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task AcknowledgeAsync(IPipelineContext<AcknowledgeEvent> pipelineContext, CancellationToken cancellationToken = default)
    {
        var projectionEvent = Guard.AgainstNull(pipelineContext).Pipeline.State.GetProjectionEvent();

        await _lock.WaitAsync(cancellationToken);

        try
        {
            var projectionExecutionContext = _projectionExecutionContexts.First(item => item.Projection.Name.Equals(projectionEvent.Projection.Name));

            await projectionExecutionContext.Lock.WaitAsync(cancellationToken);

            try
            {
                projectionExecutionContext.RemovePrimitiveEvent(projectionEvent.PrimitiveEvent);
            }
            finally
            {
                projectionExecutionContext.Lock.Release();
            }
        }
        finally
        {
            _lock.Release();
        }

        projectionEvent.Projection.Commit(projectionEvent.PrimitiveEvent.SequenceNumber!.Value);

        await Task.CompletedTask;
    }

    private async Task GetProjectionJournalAsync(ProjectionExecutionContext projectionExecutionContext)
    {
        // This would get the next batch of primitive event details for the service te return.
        // Include only event types that are handled in this process (IProjectionConfiguration).
        // Once entire batch has completed, update the sequence number for the projections.
        // This is a naive implementation and should be replaced with a more efficient one in actual implementations.

        if (!projectionExecutionContext.IsEmpty)
        {
            return;
        }

        foreach (var primitiveEvent in (await _primitiveEventStore.GetCommittedPrimitiveEventsAsync(projectionExecutionContext.Projection.SequenceNumber + 1)).OrderBy(item => item.SequenceNumber))
        {
            var index = Math.Abs((primitiveEvent.CorrelationId ?? primitiveEvent.Id).GetHashCode()) % _managedThreadIds.Length;
            var managedThreadId = _managedThreadIds[index];

            logger.LogDebug($"[{primitiveEvent.Id}] : hash = {primitiveEvent.Id.GetHashCode()} / index = {index} / managed thread id = {managedThreadId}");

            projectionExecutionContext.AddPrimitiveEvent(primitiveEvent, managedThreadId);
        }
    }

    private class ProjectionExecutionContext(Projection projection, IEnumerable<TimeSpan> backoffDurations)
    {
        private readonly TimeSpan[] _durations = Guard.AgainstEmpty(backoffDurations).ToArray();

        private readonly Dictionary<int, List<PrimitiveEvent>> _threadPrimitiveEvents = [];
        private int _durationIndex;
        private DateTimeOffset _backoffTillDate = DateTimeOffset.MinValue;

        public bool IsEmpty => _threadPrimitiveEvents.Values.All(list => list.Count == 0);
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public Projection Projection { get; } = projection;

        public void AddPrimitiveEvent(PrimitiveEvent primitiveEvent, int managedThreadId)
        {
            if (Lock.CurrentCount > 0)
            {
                throw new ApplicationException($"Lock required before invoking '{nameof(AddPrimitiveEvent)}'.");
            }

            if (!_threadPrimitiveEvents.TryGetValue(managedThreadId, out var primitiveEvents))
            {
                primitiveEvents = [];

                _threadPrimitiveEvents.Add(managedThreadId, primitiveEvents);
            }

            primitiveEvents.Add(Guard.AgainstNull(primitiveEvent));
        }

        public void Backoff(DateTimeOffset? deferredUntil = null)
        {
            if (deferredUntil.HasValue)
            {
                _backoffTillDate = deferredUntil.Value;
                return;
            }

            if (_durationIndex >= _durations.Length)
            {
                _durationIndex = _durations.Length - 1;
            }

            _backoffTillDate = DateTimeOffset.UtcNow.Add(_durations[_durationIndex++]);
        }

        public void RemovePrimitiveEvent(PrimitiveEvent primitiveEvent)
        {
            if (Lock.CurrentCount > 0)
            {
                throw new ApplicationException($"Lock required before invoking '{nameof(RemovePrimitiveEvent)}'.");
            }

            foreach (var list in _threadPrimitiveEvents.Values)
            {
                list.RemoveAll(e => e.SequenceNumber == primitiveEvent.SequenceNumber);
            }
        }

        public void Resume()
        {
            _backoffTillDate = DateTimeOffset.MinValue;
            _durationIndex = 0;
        }

        public PrimitiveEvent? RetrievePrimitiveEvent(int managedThreadId)
        {
            if (Lock.CurrentCount > 0)
            {
                throw new ApplicationException($"Lock required before invoking '{nameof(RetrievePrimitiveEvent)}'.");
            }

            if (!_threadPrimitiveEvents.TryGetValue(managedThreadId, out var primitiveEvents))
            {
                primitiveEvents = [];

                _threadPrimitiveEvents.Add(managedThreadId, primitiveEvents);
            }

            return primitiveEvents.OrderBy(item => item.SequenceNumber).FirstOrDefault();
        }

        public bool IsBackingOff()
        {
            return _backoffTillDate > DateTimeOffset.UtcNow;
        }
    }
}