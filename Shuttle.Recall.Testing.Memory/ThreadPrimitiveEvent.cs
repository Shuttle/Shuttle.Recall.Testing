using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Memory;

public class ThreadPrimitiveEvent(int managedThreadId, PrimitiveEvent primitiveEvent)
{
    public int ManagedThreadId { get; } = managedThreadId;
    public PrimitiveEvent PrimitiveEvent { get; } = Guard.AgainstNull(primitiveEvent);
}