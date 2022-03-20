using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.PostProcessors
{
    public class StopMonitoringTaskProcessor : IScopedService
    {
        private readonly WorkerService _workerServic;

        public StopMonitoringTaskProcessor(WorkerService workerService)
        {
            _workerServic = workerService;
        }

        public async Task StopMonitorAsync(string monitorId)
        {
            await _workerServic.StopMonitorAsync(monitorId);
        }

        public class StopMonitoringAfterMonitorDisabled : IRequestPostProcessor<CreateOrUpdateMonitorCmd>
        {
            private readonly StopMonitoringTaskProcessor _changeProcessor;

            public StopMonitoringAfterMonitorDisabled(StopMonitoringTaskProcessor changeProcessor)
            {
                _changeProcessor = changeProcessor;
            }

            public async Task Process(CreateOrUpdateMonitorCmd request, Unit response, CancellationToken cancellationToken)
            {
                //Stop monitoring if the monitor is not enabled anymore
                if (!request.Data.Enabled) await _changeProcessor.StopMonitorAsync(request.Data.Id);
            }
        }

        public class StopMonitoringAfterMonitorDeleted : IRequestPostProcessor<DeleteMonitorCmd>
        {
            private readonly StopMonitoringTaskProcessor _changeProcessor;

            public StopMonitoringAfterMonitorDeleted(StopMonitoringTaskProcessor changeProcessor)
            {
                _changeProcessor = changeProcessor;
            }

            public async Task Process(DeleteMonitorCmd request, Unit response, CancellationToken cancellationToken)
            {
                //Stop monitoring when a monitor was deleted
                await _changeProcessor.StopMonitorAsync(request.MonitorId);
            }
        }
    }
}
