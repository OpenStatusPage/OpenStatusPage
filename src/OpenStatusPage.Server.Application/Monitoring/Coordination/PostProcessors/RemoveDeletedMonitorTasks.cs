using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination.PostProcessors
{
    public class RemoveDeletedMonitorTasks : IRequestPostProcessor<DeleteMonitorCmd>
    {
        private readonly TaskCoordinationService _taskCoordinationService;

        public RemoveDeletedMonitorTasks(TaskCoordinationService taskCoordinationService)
        {
            _taskCoordinationService = taskCoordinationService;
        }

        public async Task Process(DeleteMonitorCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _taskCoordinationService.RemoveDeletedMonitorAssignmentsAsync(request.MonitorId, cancellationToken);
        }
    }
}
