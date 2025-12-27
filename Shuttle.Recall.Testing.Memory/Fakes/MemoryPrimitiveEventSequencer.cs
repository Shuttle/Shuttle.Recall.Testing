using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Memory.Fakes;

public class MemoryPrimitiveEventSequencer(IPrimitiveEventStore primitiveEventStore) : IPrimitiveEventSequencer
{
    private readonly PrimitiveEventStore _primitiveEventStore = (PrimitiveEventStore)Guard.AgainstNull(primitiveEventStore);

    public async ValueTask<bool> SequenceAsync(CancellationToken cancellationToken = default)
    {
        var result = false;
        var sequenceNumber = await GetMaxSequenceNumberAsync(cancellationToken);

        var primitiveEventJournals = _primitiveEventStore.Store.Values
            .SelectMany(journals =>
                journals.Where(item => item is { Committed: true, PrimitiveEvent.SequenceNumber: null }))
            .ToList();

        foreach (var primitiveEventJournal in primitiveEventJournals)
        {
            result = true;
            sequenceNumber++;
            primitiveEventJournal.PrimitiveEvent.SequenceNumber = sequenceNumber;
        }

        return result;
    }

    public ValueTask<long> GetMaxSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_primitiveEventStore.Store.Values.Any()
            ? _primitiveEventStore.Store.Values.Max(journals =>
                journals
                    .Where(item => item.Committed)
                    .Select(item => item.PrimitiveEvent.SequenceNumber ?? 0)
                    .DefaultIfEmpty(0)
                    .Max())
            : 0);
    }
}