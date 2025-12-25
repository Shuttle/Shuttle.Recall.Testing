using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Recall.Testing;

internal class FailureFixtureHostedService(IOptions<PipelineOptions> pipelineOptions) : IHostedService
{
    private readonly PipelineOptions _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
    private readonly Type _eventProcessingPipelineType = typeof(EventProcessingPipeline);
    private readonly FailureFixtureObserver _failureFixtureObserver = new(); // need a singleton for FixtureObserver._failedBefore

    private Task PipelineCreated(PipelineEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (eventArgs.Pipeline.GetType() == _eventProcessingPipelineType)
        {
            eventArgs.Pipeline.AddObserver(_failureFixtureObserver);
        }

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated += PipelineCreated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated -= PipelineCreated;

        return Task.CompletedTask;
    }
}