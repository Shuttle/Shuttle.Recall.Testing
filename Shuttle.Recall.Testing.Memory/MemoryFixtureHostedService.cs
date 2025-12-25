using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;

namespace Shuttle.Recall.Testing.Memory;

internal class MemoryFixtureHostedService(IOptions<PipelineOptions> pipelineOptions) : IHostedService
{
    private readonly PipelineOptions _pipelineOptions = Guard.AgainstNull(Guard.AgainstNull(pipelineOptions).Value);
    private readonly Type _eventProcessorStartupPipelineType = typeof(EventProcessorStartupPipeline);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated += PipelineCreated;

        return Task.CompletedTask;
    }

    private Task PipelineCreated(PipelineEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (eventArgs.Pipeline.GetType() == _eventProcessorStartupPipelineType)
        {
            eventArgs.Pipeline.AddObserver<MemoryFixtureStartupObserver>();
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _pipelineOptions.PipelineCreated -= PipelineCreated;

        await Task.CompletedTask;
    }
}