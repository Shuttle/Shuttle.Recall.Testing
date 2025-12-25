using Shuttle.Core.Contract;

namespace Shuttle.Recall.Testing.Memory.Fakes;

public class PrimitiveEventJournal(PrimitiveEvent primitiveEvent)
{
    public PrimitiveEvent PrimitiveEvent { get; } = Guard.AgainstNull(primitiveEvent);
    public bool Committed { get; private set; }

    public void Commit()
    {
        Committed = true;
    }
}