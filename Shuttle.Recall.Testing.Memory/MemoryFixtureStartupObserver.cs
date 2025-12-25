using Shuttle.Core.Contract;
using Shuttle.Core.Pipelines;
using Shuttle.Core.Threading;
using Shuttle.Recall.Testing.Memory.Fakes;

namespace Shuttle.Recall.Testing.Memory;

public class MemoryFixtureStartupObserver(IProjectionService projectionService) : IPipelineObserver<ThreadPoolsStarted>
{
    private readonly IProjectionService _projectionService = Guard.AgainstNull(projectionService);

    public async Task ExecuteAsync(IPipelineContext<ThreadPoolsStarted> pipelineContext, CancellationToken cancellationToken = default)
    {
        if (_projectionService is not MemoryProjectionService service)
        {
            throw new InvalidOperationException($"The projection event service must be of type '{typeof(MemoryProjectionService).FullName}'; instead found type '{_projectionService.GetType().FullName}'.");
        }

        await service.StartupAsync(Guard.AgainstNull(Guard.AgainstNull(pipelineContext).Pipeline.State.Get<IProcessorThreadPool>("EventProcessorThreadPool")));
    }
}