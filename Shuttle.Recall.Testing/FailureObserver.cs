using Shuttle.Core.Pipelines;
using Shuttle.Recall.Testing.Order;

namespace Shuttle.Recall.Testing;

internal class FailureFixtureObserver : IPipelineObserver<EventHandled>
{
    private bool _failedBefore;

    public async Task ExecuteAsync(IPipelineContext<EventHandled> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (pipelineContext.Pipeline.State.GetDomainEvent().Event is not ItemAdded itemAdded)
        {
            return;
        }

        if (itemAdded.Product.Equals("item-3") && !_failedBefore)
        {
            _failedBefore = true;

            var message = $"[{nameof(FailureFixtureObserver)}] : One-time failure of 'item-3'.";

            Console.WriteLine(message);

            throw new ApplicationException(message);
        }

        await Task.CompletedTask;
    }
}