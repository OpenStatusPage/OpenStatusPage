using MediatR;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;

namespace OpenStatusPage.Server.Application.ResourceManagement.PostProcessors
{
    public class OptimizeDatababaseWithNewSettings : IRequestPostProcessor<CreateOrUpdateApplicationSettingsCmd>
    {
        private readonly DatabaseOptimizer _databaseOptimizer;

        public OptimizeDatababaseWithNewSettings(DatabaseOptimizer databaseOptimizer)
        {
            _databaseOptimizer = databaseOptimizer;
        }

        public async Task Process(CreateOrUpdateApplicationSettingsCmd request, Unit response, CancellationToken cancellationToken)
        {
            _databaseOptimizer.TriggerDoWork();
        }
    }
}
