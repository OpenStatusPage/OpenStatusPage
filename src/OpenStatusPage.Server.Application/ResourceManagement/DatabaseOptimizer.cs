using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Notifications.History.Commands;
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

            var statusHistoryRecordGroups = (await _scopedMediator.Send(new StatusHistoriesQuery()))?.HistoryRecords
                .GroupBy(x => x.MonitorId);

            if (statusHistoryRecordGroups != null)
            {
                foreach (var statusHistoryRecordGroup in statusHistoryRecordGroups)
                {
                    //Remove anything before x days ago or only until the latest status. The latest status could be in the removal range.
                    var safeRemoveBefore = new[] { statusHistoryRecordGroup.Max(x => x.FromUtc), removeStatusHistoryBefore }.Min();

                    foreach (var statusHistoryRecord in statusHistoryRecordGroup)
                    {
                        if (statusHistoryRecord.FromUtc < safeRemoveBefore)
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
            }

            //Process status notification history
            var notificationHistoryRecordGroups = (await _scopedMediator.Send(new NotificationHistoriesQuery()))?.NotificationHistoryRecords
                .GroupBy(x => x.MonitorId);

            if (notificationHistoryRecordGroups != null)
            {
                foreach (var notificationHistoryRecordGroup in notificationHistoryRecordGroups)
                {
                    //Remove anything before x days ago or only until the latest status. The latest status could be in the removal range.
                    //Same range as status history records, but we might need to keep and even older notification if the status has not changed for a while.
                    var safeRemoveBefore = new[] { notificationHistoryRecordGroup.Max(x => x.StatusUtc), removeStatusHistoryBefore }.Min();

                    foreach (var notificationHistoryRecord in notificationHistoryRecordGroup)
                    {
                        if (notificationHistoryRecord.StatusUtc < safeRemoveBefore)
                        {
                            try
                            {
                                _logger.LogDebug($"Dropping notification history record({notificationHistoryRecord.MonitorId}|{notificationHistoryRecord.StatusUtc}) from history.");

                                await _clusterService.ReplicateAsync(new DeleteNotificationHistoryRecordCmd
                                {
                                    MonitorId = notificationHistoryRecord.MonitorId,
                                    StatusUtc = notificationHistoryRecord.StatusUtc
                                });
                            }
                            catch
                            {
                            }
                        }
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
