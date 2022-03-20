using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Domain.Entities.Configuration;

namespace OpenStatusPage.Server.Application.Configuration.Commands
{
    public class ApplicationSettingsQuery : RequestBase<ApplicationSettingsQuery.Response>
    {
        public class Handler : IRequestHandler<ApplicationSettingsQuery, Response>
        {
            private readonly ApplicationSettingsService _applicationSettingsService;

            public Handler(ApplicationSettingsService applicationSettingsService)
            {
                _applicationSettingsService = applicationSettingsService;
            }

            public async Task<Response> Handle(ApplicationSettingsQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    ApplicationSettings = (await _applicationSettingsService.Get().AsNoTracking().FirstOrDefaultAsync(cancellationToken))!
                };
            }
        }

        public class Response
        {
            public ApplicationSettings ApplicationSettings { get; set; }
        }
    }
}
