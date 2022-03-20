using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.PostProcessors
{
    public class TrackMonitorVersionIntervals : IRequestPostProcessor<CreateOrUpdateMonitorCmd>
    {
        private readonly StatusTimelineService _statusTimelineService;

        public TrackMonitorVersionIntervals(StatusTimelineService statusTimelineService)
        {
            _statusTimelineService = statusTimelineService;
        }

        public async Task Process(CreateOrUpdateMonitorCmd request, Unit response, CancellationToken cancellationToken)
        {
            await _statusTimelineService.AddMonitorIntervalAsync(request.Data.Id, request.Data.Version, request.Data.Interval, cancellationToken);
        }
    }
}
