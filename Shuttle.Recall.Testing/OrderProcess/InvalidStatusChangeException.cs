namespace Shuttle.Recall.Testing.OrderProcess;

public class InvalidStatusChangeException : Exception
{
    public InvalidStatusChangeException(string message) : base(message)
    {
    }

    public InvalidStatusChangeException()
    {
    }
}