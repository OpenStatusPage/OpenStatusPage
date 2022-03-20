using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination.PostProcessors
{
    public class CancelStaleAssignmentsProcessor : IRequestPostProcessor<UpdateServiceStatusCmd>
    {
        private readonly TaskCoordinationService _taskCoordinationService;

        public CancelStaleAssignmentsProcessor(TaskCoordinationService taskCoordinationService)
        {
            _taskCoordinationService = taskCoordinationService;
        }

        public async Task Process(UpdateServiceStatusCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _taskCoordinationService.RemoveStaleAssignmentsAsync(request.MonitorId, request.TaskAssignmentId, cancellationToken);
        }
    }
}
