using System.Transactions;
using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Memory.Fakes;

public class MemoryPrimitiveEventRepository(IPrimitiveEventStore primitiveEventStore) : IPrimitiveEventRepository
{
    private readonly IPrimitiveEventStore _primitiveEventStore = Guard.AgainstNull(primitiveEventStore);


    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _primitiveEventStore.RemoveAggregateAsync(id);
    }

    public async ValueTask<long> SaveAsync(IEnumerable<PrimitiveEvent> primitiveEvents, CancellationToken cancellationToken = default)
    {
        long sequenceNumber = 0;

        var primitiveEventJournals = primitiveEvents.Select(item=>new PrimitiveEventJournal(item)).ToList();

        foreach (var primitiveEventJournal in primitiveEventJournals)
        {
            sequenceNumber = await _primitiveEventStore.AddAsync(primitiveEventJournal);
        }

        if (Transaction.Current != null)
        {
            Transaction.Current.EnlistVolatile(new PrimitiveEventJournalResourceManager(_primitiveEventStore, primitiveEventJournals), EnlistmentOptions.None);
        }

        return sequenceNumber;
    }

    public async ValueTask<long> GetMaxSequenceNumberAsync(CancellationToken cancellationToken = default)
    {
        return await _primitiveEventStore.GetMaxSequenceNumberAsync();
    }

    public async Task<IEnumerable<PrimitiveEvent>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _primitiveEventStore.GetAsync(id);
    }

    public async ValueTask<long> GetSequenceNumberAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _primitiveEventStore.GetSequenceNumberAsync(id);
    }
}