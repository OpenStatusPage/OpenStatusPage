using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.Configuration
{
    public class ApplicationSettingsSnapshotProvider : ISnapshotDataProvider
    {
        private readonly IMediator _mediator;
        private readonly ApplicationDbContext _applicationDbContext;

        public ApplicationSettingsSnapshotProvider(IMediator mediator, ApplicationDbContext applicationDbContext)
        {
            _mediator = mediator;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<MessageBase>();

            var appSettings = (await _mediator.Send(new ApplicationSettingsQuery(), cancellationToken))?.ApplicationSettings;

            if (appSettings != null)
            {
                result.Add(new CreateOrUpdateApplicationSettingsCmd()
                {
                    Data = appSettings
                });
            }

            return result;
        }

        [SnapshotApplyDataOrder(30)]
        public async Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default)
        {
            var appSettings = (await _mediator.Send(new ApplicationSettingsQuery(), cancellationToken))?.ApplicationSettings;

            //Local entity does not existing in the snapshot data from the leader anymore, remove it
            if (appSettings != null && data.Any(x => x is CreateOrUpdateApplicationSettingsCmd createOrUpdate && createOrUpdate.Data.Id == appSettings.Id))
            {
                _applicationDbContext.Remove(appSettings);
                await _applicationDbContext.SaveChangesAsync(cancellationToken);
            }

            foreach (var message in data)
            {
                switch (message)
                {
                    case CreateOrUpdateApplicationSettingsCmd createOrUpdate:
                    {
                        await _mediator.Send(createOrUpdate, cancellationToken);
                        break;
                    }

                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
