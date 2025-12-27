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

    public async Task SaveAsync(IEnumerable<PrimitiveEvent> primitiveEvents, CancellationToken cancellationToken = default)
    {
        var primitiveEventJournals = primitiveEvents.Select(item=>new PrimitiveEventJournal(item)).ToList();

        foreach (var primitiveEventJournal in primitiveEventJournals)
        {
            await _primitiveEventStore.AddAsync(primitiveEventJournal);
        }

        if (Transaction.Current != null)
        {
            Transaction.Current.EnlistVolatile(new PrimitiveEventJournalResourceManager(_primitiveEventStore, primitiveEventJournals), EnlistmentOptions.None);
        }
    }

    public async Task<IEnumerable<PrimitiveEvent>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _primitiveEventStore.GetAsync(id);
    }
}