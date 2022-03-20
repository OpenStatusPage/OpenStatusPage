using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.Notifications.History;

namespace OpenStatusPage.Server.Application.Notifications.History.Commands
{
    public class NotificationHistoriesQuery : RequestBase<NotificationHistoriesQuery.Response>
    {
        public QueryExtension<NotificationHistoryRecord> Query { get; set; }

        public class Handler : IRequestHandler<NotificationHistoriesQuery, Response>
        {
            private readonly NotificationHistoryService _notificationHistoryService;

            public Handler(NotificationHistoryService notificationHistoryService)
            {
                _notificationHistoryService = notificationHistoryService;
            }

            public async Task<Response> Handle(NotificationHistoriesQuery request, CancellationToken cancellationToken)
            {
                var records = await _notificationHistoryService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                //Fix for SQLite date time undefined kind behavior
                records.ForEach(record => record.StatusUtc = DateTime.SpecifyKind(record.StatusUtc, DateTimeKind.Utc));

                return new Response
                {
                    NotificationHistoryRecords = records
                };
            }
        }

        public class Response
        {
            public List<NotificationHistoryRecord> NotificationHistoryRecords { get; set; }
        }
    }
}
