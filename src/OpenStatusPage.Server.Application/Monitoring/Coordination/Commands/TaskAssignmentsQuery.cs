using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination.Commands
{
    public class TaskAssignmentsQuery : RequestBase<TaskAssignmentsQuery.Response>
    {
        public class Handler : IRequestHandler<TaskAssignmentsQuery, Response>
        {
            private readonly TaskCoordinationService _taskCoordinationService;

            public Handler(TaskCoordinationService taskCoordinationService)
            {
                _taskCoordinationService = taskCoordinationService;
            }

            public async Task<Response> Handle(TaskAssignmentsQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    TaskAssignments = await _taskCoordinationService.GetTaskAssignmentsAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<TaskAssignmentCmd> TaskAssignments { get; set; }
        }
    }
}
