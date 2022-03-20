using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.Coordination.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.PostProcessors
{
    public class TaskAssignmentWorkerProcessor : IRequestPostProcessor<TaskAssignmentCmd>
    {
        private readonly WorkerService _workerService;

        public TaskAssignmentWorkerProcessor(WorkerService workerService)
        {
            _workerService = workerService;
        }

        public async Task Process(TaskAssignmentCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _workerService.HandleTaskAssignmentAsync(request, cancellationToken);
        }
    }
}
