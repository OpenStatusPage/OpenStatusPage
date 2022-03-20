using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands
{
    public class ServiceStatusQuery : RequestBase<ServiceStatus?>
    {
        public string MonitorId { get; set; }

        public long MonitorVersion { get; set; }

        /// <summary>
        /// Ask for a specific moment in time. Default (null) means latest status
        /// </summary>
        public DateTimeOffset? At { get; set; }

        public class Handler : IRequestHandler<ServiceStatusQuery, ServiceStatus?>
        {
            private readonly StatusTimelineService _statusTimelineService;

            public Handler(StatusTimelineService statusTimelineService)
            {
                _statusTimelineService = statusTimelineService;
            }

            public async Task<ServiceStatus?> Handle(ServiceStatusQuery request, CancellationToken cancellationToken)
            {
                return await _statusTimelineService.GetServiceStatusAsync(request.MonitorId, request.MonitorVersion, request.At, cancellationToken);
            }
        }
    }
}
