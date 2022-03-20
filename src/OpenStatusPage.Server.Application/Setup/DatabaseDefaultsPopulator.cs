using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.StatusPages.Commands;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Setup
{
    public class DatabaseDefaultsPopulator : ISingletonService, IHostedService
    {
        private readonly ClusterService _clusterService;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly EnvironmentSettings _environmentSettings;

        public DatabaseDefaultsPopulator(ClusterService clusterService,
                                         ScopedMediatorExecutor scopedMediator,
                                         EnvironmentSettings environmentSettings)
        {
            _clusterService = clusterService;
            _scopedMediator = scopedMediator;
            _environmentSettings = environmentSettings;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //If the instance starting is a cluster founder (no connect endpoints) ensure the database defaults are created before serving requests
            if (_environmentSettings.ConnectEndpoints.Count == 0)
            {
                //Create database defaults after cluster has started.
                _clusterService.OnInitialized += (sender, args) => EnsureDefaultValuesAsync().GetAwaiter().GetResult();
            }

            return Task.CompletedTask;
        }

        public async Task EnsureDefaultValuesAsync()
        {
            try
            {
                var existingAppsettings = await _scopedMediator.Send(new ApplicationSettingsQuery());

                //If we have existing application settings, the setup is already completed
                if (existingAppsettings?.ApplicationSettings != null) return;

                var defaultStatusPageId = Guid.NewGuid().ToString();

                //Create default status page
                var pageCreated = await _clusterService.ReplicateAsync(new CreateOrUpdateStatusPageCmd()
                {
                    Data = new()
                    {
                        Id = defaultStatusPageId,
                        Name = "OpenStatusPage",
                        Description = "This page was created automatically. \nIf you are the administrator, log into the ***[dashboard](/dashboard)*** to start configuring the system.",
                        EnableGlobalSummary = false,
                        EnableUpcomingMaintenances = false,
                        EnableIncidentTimeline = false,
                        DaysStatusHistory = 90
                    }
                });

                if (!pageCreated) throw new Exception("Unable to create default status page");

                //Create system configuration, with default status page set
                var settingsCreated = await _clusterService.ReplicateAsync(new CreateOrUpdateApplicationSettingsCmd()
                {
                    Data = new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        StatusFlushInterval = TimeSpan.FromDays(1),
                        DaysIncidentHistory = 90,
                        DaysMonitorHistory = 90,
                        DefaultStatusPageId = defaultStatusPageId
                    }
                });

                if (!settingsCreated) throw new Exception("Unable to create default status page");
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to apply databse defaults.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
