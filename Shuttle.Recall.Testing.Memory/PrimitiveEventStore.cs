using Shuttle.Core.Contract;
using Shuttle.Recall.Testing.Memory.Fakes;

namespace Shuttle.Recall.Testing.Memory;

public class PrimitiveEventStore : IPrimitiveEventStore
{
    private static long _sequenceNumber = 1;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public readonly Dictionary<Guid, List<PrimitiveEventJournal>> Store = new();

    public async Task RemoveAggregateAsync(Guid id)
    {
        await _lock.WaitAsync();

        try
        {
            Store.Remove(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask<long> AddAsync(PrimitiveEventJournal primitiveEventJournal)
    {
        Guard.AgainstNull(primitiveEventJournal);

        await _lock.WaitAsync();

        try
        {
            if (!Store.ContainsKey(Guard.AgainstNull(primitiveEventJournal.PrimitiveEvent).Id))
            {
                Store.Add(primitiveEventJournal.PrimitiveEvent.Id, []);
            }

            primitiveEventJournal.PrimitiveEvent.SequenceNumber = _sequenceNumber++;

            Store[primitiveEventJournal.PrimitiveEvent.Id].Add(primitiveEventJournal);

            return primitiveEventJournal.PrimitiveEvent.SequenceNumber!.Value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<PrimitiveEvent>> GetAsync(Guid id)
    {
        await _lock.WaitAsync();

        try
        {
            return await Task.FromResult(Store.TryGetValue(id, out var value) ? value.Select(item => item.PrimitiveEvent).ToList() : new());
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask<long> GetSequenceNumberAsync(Guid id)
    {
        await _lock.WaitAsync();

        try
        {
            return Store.TryGetValue(id, out var value) ? value.Max(item => item.PrimitiveEvent.SequenceNumber!.Value) : 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<PrimitiveEvent>> GetCommittedPrimitiveEventsAsync(long sequenceNumber)
    {
        await _lock.WaitAsync();

        try
        {
            var primitiveEventJournals = Store.Values.SelectMany(list => list).ToList();

            var uncommittedPrimitiveEventJournal = primitiveEventJournals
                .OrderBy(item => item.PrimitiveEvent.SequenceNumber)
                .FirstOrDefault(item => !item.Committed);

            var minUncommittedSequenceNumber = uncommittedPrimitiveEventJournal != null ? uncommittedPrimitiveEventJournal.PrimitiveEvent.SequenceNumber : long.MaxValue;

            return primitiveEventJournals
                .Where(item => item.PrimitiveEvent.SequenceNumber >= sequenceNumber && item.PrimitiveEvent.SequenceNumber <= minUncommittedSequenceNumber)
                .Select(item => item.PrimitiveEvent);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveEventAsync(Guid id, Guid eventId)
    {
        await _lock.WaitAsync();

        try
        {
            if (!Store.TryGetValue(id, out var value))
            {
                return;
            }

            value.RemoveAll(item => item.PrimitiveEvent.Id == eventId);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> GetMaxSequenceNumberAsync()
    {
        return await Task.FromResult(_sequenceNumber);
    }
}