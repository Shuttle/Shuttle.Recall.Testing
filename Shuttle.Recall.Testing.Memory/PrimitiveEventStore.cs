using Shuttle.Core.Contract;
using Shuttle.Recall.Testing.Memory.Fakes;

namespace Shuttle.Recall.Testing.Memory;

public class PrimitiveEventStore : IPrimitiveEventStore
{
    private static long _sequenceNumber = 1;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<Guid, List<PrimitiveEventJournal>> _store = new();

    public async Task RemoveAggregateAsync(Guid id)
    {
        await _lock.WaitAsync();

        try
        {
            _store.Remove(id);
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
            if (!_store.ContainsKey(Guard.AgainstNull(primitiveEventJournal.PrimitiveEvent).Id))
            {
                _store.Add(primitiveEventJournal.PrimitiveEvent.Id, []);
            }

            primitiveEventJournal.PrimitiveEvent.SequenceNumber = _sequenceNumber++;

            _store[primitiveEventJournal.PrimitiveEvent.Id].Add(primitiveEventJournal);

            return primitiveEventJournal.PrimitiveEvent.SequenceNumber;
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
            return await Task.FromResult(_store.TryGetValue(id, out var value) ? value.Select(item => item.PrimitiveEvent).ToList() : new());
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
            return _store.TryGetValue(id, out var value) ? value.Max(item => item.PrimitiveEvent.SequenceNumber) : 0;
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
            var primitiveEventJournals = _store.Values.SelectMany(list => list).ToList();

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
            if (!_store.TryGetValue(id, out var value))
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