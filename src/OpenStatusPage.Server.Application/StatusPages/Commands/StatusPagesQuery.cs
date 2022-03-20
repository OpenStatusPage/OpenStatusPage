using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.StatusPages;

namespace OpenStatusPage.Server.Application.StatusPages.Commands
{
    public class StatusPagesQuery : RequestBase<StatusPagesQuery.Response>
    {
        public QueryExtension<StatusPage> Query { get; set; }

        public class Handler : IRequestHandler<StatusPagesQuery, Response>
        {
            private readonly StatusPageService _statusPageService;

            public Handler(StatusPageService statusPageService)
            {
                _statusPageService = statusPageService;
            }

            public async Task<Response> Handle(StatusPagesQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    StatusPages = await _statusPageService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<StatusPage> StatusPages { get; set; }
        }
    }
}
