using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;

namespace OpenStatusPage.Server.Application.Notifications.Providers.Commands
{
    public class NotificationProvidersQuery : RequestBase<NotificationProvidersQuery.Response>
    {
        public QueryExtension<NotificationProvider> Query { get; set; }

        public class Handler : IRequestHandler<NotificationProvidersQuery, Response>
        {
            private readonly NotificationProviderService _notificationProviderService;

            public Handler(NotificationProviderService notificationProviderService)
            {
                _notificationProviderService = notificationProviderService;
            }

            public async Task<Response> Handle(NotificationProvidersQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    NotificationProviders = await _notificationProviderService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<NotificationProvider> NotificationProviders { get; set; }
        }
    }
}
