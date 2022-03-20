using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.ResourceManagement
{
    public class DatabaseOptimizer : ISingletonService, IHostedService, IDisposable
    {
        protected static readonly TimeSpan _interval = TimeSpan.FromHours(12);

        private readonly ILogger<DatabaseOptimizer> _logger;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly ClusterService _clusterService;
        private Timer _timer = null!;

        public DatabaseOptimizer(ILogger<DatabaseOptimizer> logger, ScopedMediatorExecutor scopedMediator, ClusterService clusterService)
        {
            _logger = logger;
            _scopedMediator = scopedMediator;
            _clusterService = clusterService;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer((state) => _ = Task.Run(DoWorkAsync), null, TimeSpan.Zero, _interval);

            return Task.CompletedTask;
        }

        public void TriggerDoWork()
        {
            _timer.Change(TimeSpan.Zero, _interval);
        }

        protected async Task DoWorkAsync()
        {
            //Only perform work if leader and operational
            if (!_clusterService.IsLocalLeader() || !_clusterService.IsOperational) return;

            var appsettings = (await _scopedMediator.Send(new ApplicationSettingsQuery()))?.ApplicationSettings;

            if (appsettings == null) return;

            //Process incidents
            var incidents = (await _scopedMediator.Send(new IncidentsQuery()
            {
                Query = new(query => query.Where(x => x.Until.HasValue))
            }))?.Incidents;

            if (incidents != null)
            {
                //Do the date comparison in memory because of SQLite not supporting datetimeoffset
                var removeIncidentsBefore = DateTimeOffset.UtcNow.UtcDateTime.AddDays(-appsettings.DaysIncidentHistory);

                foreach (var incident in incidents.Where(x => x.Until.Value < removeIncidentsBefore))
                {
                    try
                    {
                        _logger.LogDebug($"Dropping incident({incident.Id}) from history.");

                        await _clusterService.ReplicateAsync(new DeleteIncidentCmd
                        {
                            IncidentId = incident.Id
                        });
                    }
                    catch
                    {
                    }
                }
            }

            //Process monitor history
            var removeStatusHistoryBefore = DateTimeOffset.UtcNow.UtcDateTime.AddDays(-appsettings.DaysMonitorHistory);

            var statusHistoryRecords = (await _scopedMediator.Send(new StatusHistoriesQuery()
            {
                Query = new(query => query.Where(x => x.FromUtc < removeStatusHistoryBefore))
            }))?.HistoryRecords;

            if (statusHistoryRecords != null)
            {
                foreach (var statusHistoryRecord in statusHistoryRecords)
                {
                    try
                    {
                        _logger.LogDebug($"Dropping status history record({statusHistoryRecord.MonitorId}|{statusHistoryRecord.FromUtc}) from history.");

                        await _clusterService.ReplicateAsync(new DeleteStatusHistoryRecordCmd
                        {
                            MonitorId = statusHistoryRecord.MonitorId,
                            UtcFrom = statusHistoryRecord.FromUtc
                        });
                    }
                    catch
                    {
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
