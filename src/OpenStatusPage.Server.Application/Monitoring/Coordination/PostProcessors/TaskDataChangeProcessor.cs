using MediatR;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination.PostProcessors
{
    public class TaskDataChangeProcessor : IScopedService
    {
        private readonly TaskCoordinationService _taskCoordinationService;

        public TaskDataChangeProcessor(TaskCoordinationService taskCoordinationService)
        {
            _taskCoordinationService = taskCoordinationService;
        }

        public void DebouncedRedistribution()
        {
            _taskCoordinationService.DebouncedRedistribution();
        }

        public class TaskAssignmentsAfterMonitorUpdate : IRequestPostProcessor<CreateOrUpdateMonitorCmd>
        {
            private readonly TaskDataChangeProcessor _changeProcessor;

            public TaskAssignmentsAfterMonitorUpdate(TaskDataChangeProcessor changeProcessor)
            {
                _changeProcessor = changeProcessor;
            }

            public async Task Process(CreateOrUpdateMonitorCmd request, Unit response, CancellationToken cancellationToken)
            {
                _changeProcessor.DebouncedRedistribution();
            }
        }
    }
}
