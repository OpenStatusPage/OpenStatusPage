using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;

namespace OpenStatusPage.Server.Application.StatusHistory.Commands
{
    public class StatusHistoriesQuery : RequestBase<StatusHistoriesQuery.Response>
    {
        public QueryExtension<StatusHistoryRecord> Query { get; set; }

        public class Handler : IRequestHandler<StatusHistoriesQuery, Response>
        {
            private readonly StatusHistoryService _statusHistoryService;

            public Handler(StatusHistoryService statusHistoryService)
            {
                _statusHistoryService = statusHistoryService;
            }

            public async Task<Response> Handle(StatusHistoriesQuery request, CancellationToken cancellationToken)
            {
                var records = await _statusHistoryService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                //Fix for SQLite date time undefined kind behavior
                records.ForEach(record => record.FromUtc = DateTime.SpecifyKind(record.FromUtc, DateTimeKind.Utc));

                return new Response
                {
                    HistoryRecords = records
                };
            }
        }

        public class Response
        {
            public List<StatusHistoryRecord> HistoryRecords { get; set; }
        }
    }
}
