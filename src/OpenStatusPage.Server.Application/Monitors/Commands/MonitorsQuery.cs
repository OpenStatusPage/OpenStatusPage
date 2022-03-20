using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.Monitors;

namespace OpenStatusPage.Server.Application.Monitors.Commands
{
    public class MonitorsQuery : RequestBase<MonitorsQuery.Response>
    {
        public QueryExtension<MonitorBase> Query { get; set; }

        public class Handler : IRequestHandler<MonitorsQuery, Response>
        {
            private readonly MonitorService _monitorService;

            public Handler(MonitorService monitorService)
            {
                _monitorService = monitorService;
            }

            public async Task<Response> Handle(MonitorsQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    Monitors = await _monitorService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<MonitorBase> Monitors { get; set; }
        }
    }
}
