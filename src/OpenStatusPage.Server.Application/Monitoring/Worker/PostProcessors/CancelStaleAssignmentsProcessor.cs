using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.PostProcessors
{
    public class CancelStaleMonitoringTasksProcessor : IRequestPostProcessor<UpdateServiceStatusCmd>
    {
        private readonly WorkerService _workerService;

        public CancelStaleMonitoringTasksProcessor(WorkerService workerService)
        {
            _workerService = workerService;
        }

        public async Task Process(UpdateServiceStatusCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _workerService.CancelStaleMonitoringTasksAsync(request.MonitorId, request.TaskAssignmentId, cancellationToken);
        }
    }
}
