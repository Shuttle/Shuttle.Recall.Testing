using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Memory.Fakes;

public class MemoryPrimitiveEventSequencer(IPrimitiveEventStore primitiveEventStore) : IPrimitiveEventSequencer
{
    private readonly PrimitiveEventStore _primitiveEventStore = (PrimitiveEventStore)Guard.AgainstNull(primitiveEventStore);

    public ValueTask<bool> SequenceAsync(CancellationToken cancellationToken = default)
    {
        var result = false;
        var sequenceNumber = _primitiveEventStore.Store.Values.Any()
            ? _primitiveEventStore.Store.Values.Max(journals =>
                journals
                    .Where(item => item.Committed)
                    .Select(item => item.PrimitiveEvent.SequenceNumber ?? 0)
                    .DefaultIfEmpty(0)
                    .Max())
            : 0;

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

        return ValueTask.FromResult(result);
    }
}