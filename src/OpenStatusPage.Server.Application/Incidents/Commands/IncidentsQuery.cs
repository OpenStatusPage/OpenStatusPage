using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.Incidents;

namespace OpenStatusPage.Server.Application.Incidents.Commands
{
    public class IncidentsQuery : RequestBase<IncidentsQuery.Response>
    {
        public QueryExtension<Incident> Query { get; set; }

        public class Handler : IRequestHandler<IncidentsQuery, Response>
        {
            private readonly IncidentService _incidentService;

            public Handler(IncidentService incidentService)
            {
                _incidentService = incidentService;
            }

            public async Task<Response> Handle(IncidentsQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    Incidents = await _incidentService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<Incident> Incidents { get; set; }
        }
    }
}
