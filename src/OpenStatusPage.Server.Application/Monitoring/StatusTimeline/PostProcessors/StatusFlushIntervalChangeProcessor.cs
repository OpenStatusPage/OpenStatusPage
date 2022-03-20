using MediatR;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.PostProcessors
{
    public class StatusFlushIntervalChangeProcessor : IRequestPostProcessor<CreateOrUpdateApplicationSettingsCmd>
    {
        private readonly StatusTimelineService _statusTimelineService;

        public StatusFlushIntervalChangeProcessor(StatusTimelineService statusTimelineService)
        {
            _statusTimelineService = statusTimelineService;
        }

        public async Task Process(CreateOrUpdateApplicationSettingsCmd request, Unit response, CancellationToken cancellationToken)
        {
            _statusTimelineService.TriggerStatusFlush(request.Data.StatusFlushInterval);
        }
    }
}
