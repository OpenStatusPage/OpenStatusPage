using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Commands
{
    public class MonitorTaskStatusQuery : RequestBase<MonitorTaskStatusQuery.Response>
    {
        public string MonitorId { get; set; }

        public long MonitorVersion { get; set; }

        public class Handler : IRequestHandler<MonitorTaskStatusQuery, Response>
        {
            private readonly WorkerService _workerService;

            public Handler(WorkerService workerService)
            {
                _workerService = workerService;
            }

            public async Task<Response> Handle(MonitorTaskStatusQuery request, CancellationToken cancellationToken)
            {
                var taskData = await _workerService.GetMonitorTaskStatusAsync(request.MonitorId, request.MonitorVersion, cancellationToken);

                if (taskData == default) return null;

                return new Response
                {
                    Active = taskData.Item1,
                    FirstExecutionDispatched = taskData.Item2,
                    LastExecutionDispatched = taskData.Item3,
                    NextScheduledExecution = taskData.Item4
                };
            }
        }

        public class Response
        {
            public bool? Active { get; init; }

            public DateTimeOffset? FirstExecutionDispatched { get; init; }

            public DateTimeOffset? LastExecutionDispatched { get; init; }

            public DateTimeOffset? NextScheduledExecution { get; init; }
        }
    }
}
