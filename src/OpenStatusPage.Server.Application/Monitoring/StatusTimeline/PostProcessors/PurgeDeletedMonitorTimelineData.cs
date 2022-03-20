using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.PostProcessors
{
    public class PurgeDeletedMonitorTimelineData : IRequestPostProcessor<DeleteMonitorCmd>
    {
        private readonly StatusTimelineService _statusTimelineService;

        public PurgeDeletedMonitorTimelineData(StatusTimelineService statusTimelineService)
        {
            _statusTimelineService = statusTimelineService;
        }

        public async Task Process(DeleteMonitorCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _statusTimelineService.RemoveDeletedMonitorDataAsync(request.MonitorId, cancellationToken);
        }
    }
}
