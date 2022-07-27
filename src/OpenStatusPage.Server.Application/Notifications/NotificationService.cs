using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.Notifications.History.Commands;
using OpenStatusPage.Server.Application.Notifications.Senders;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Domain.Entities.StatusHistory;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Interfaces;
using OpenStatusPage.Shared.Utilities;

namespace OpenStatusPage.Server.Application.Notifications
{
    public class NotificationService : ISingletonService, IHostedService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly ClusterService _clusterService;
        private readonly EnvironmentSettings _environmentSettings;

        protected Task SendNotificationsTask { get; set; }

        public NotificationService(ILogger<NotificationService> logger,
                                   ScopedMediatorExecutor scopedMediator,
                                   ClusterService clusterService,
                                   EnvironmentSettings environmentSettings)
        {
            _logger = logger;
            _scopedMediator = scopedMediator;
            _clusterService = clusterService;
            _environmentSettings = environmentSettings;
            _clusterService.OnClusterLeaderChanged += (sender, args) => DispatchNotifications();
        }

        public void DispatchNotifications()
        {
            //Quick pre check to see if it is worth starting the task at all. Only leaders will be able to send notifications
            if (!_clusterService.IsLocalLeader()) return;

            if (SendNotificationsTask == null || SendNotificationsTask.IsCompleted)
            {
                SendNotificationsTask = Task.Run(SendNotificationsAsync);
            }
        }

        protected async Task SendNotificationsAsync()
        {
            while (_clusterService.IsLocalLeader())
            {
                //Fetch all monitors with ther notification providers
                var monitors = (await _scopedMediator.Send(new MonitorsQuery()
                {
                    Query = new(query => query.Include(x => x.NotificationProviders))
                }))?.Monitors;

                //No monitors to send notifications about
                if (monitors == null || monitors.Count == 0) return;

                //Load the latest notification sent per monitor
                var latestNotifications = (await _scopedMediator.Send(new NotificationHistoriesQuery()
                {
                    Query = new(query => query
                        .GroupBy(x => x.MonitorId)
                        .Select(group => group
                            .OrderByDescending(notification => notification.StatusUtc)
                            .First()))
                }))?.NotificationHistoryRecords ?? new();

                try
                {
                    foreach (var monitor in monitors)
                    {
                        //Get the last notification sent for this monitor
                        var latestNotification = latestNotifications.FirstOrDefault(x => x.MonitorId == monitor.Id);

                        //Get all history events including the last sent one and after
                        var historyEvents = (await _scopedMediator.Send(new StatusHistoriesQuery()
                        {
                            Query = new(query => query
                                .Where(x => x.MonitorId == monitor.Id && (latestNotification == null || x.FromUtc >= latestNotification.StatusUtc))
                                .OrderBy(x => x.FromUtc))
                        }))?.HistoryRecords;

                        //Fetch all open incidents
                        var incidents = (await _scopedMediator.Send(new IncidentsQuery()
                        {
                            Query = new(query => query
                                .Where(x => x.AffectedServices.Contains(monitor))
                                .Include(x => x.Timeline))
                        }))?.Incidents ?? new();

                        //Send pairwise notifcations from old status and new status. Same status will be merged
                        StatusHistoryRecord? previous = null;

                        foreach (var current in historyEvents)
                        {
                            //Still the same status, continue
                            if ((previous?.Status ?? ServiceStatus.Unknown) == current.Status) continue;

                            //Send notification if it is the first one ever for the monitor, or the previous one (initally the last send notification) was loaded
                            if (latestNotification == null || previous != null)
                            {
                                var machingIncidents = incidents
                                    .Where(x => ((DateTimeOffset)current.FromUtc).IsInRangeInclusiveNullable(x.From, x.Until))
                                    .ToList();

                                var isMaintenance = machingIncidents
                                    .Any(x => x.Timeline.Count > 0 && x.Timeline.Last().Severity == IncidentSeverity.Maintenance);

                                //Do not send notification during maintenances
                                if (!isMaintenance)
                                {
                                    await SendNotificationAsync(monitor, previous, current);
                                }
                                else
                                {
                                    _logger.LogDebug($"Skipping status notification on monitor({monitor.Name}|{monitor.Id}) for {current.FromUtc} due to maintenance window.");
                                }
                            }

                            previous = current;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical($"Failed to deliver some notifications. Exception details:\n{ex.Message}");

                    //Something went wrong, like a provider being unable to deliver the notification right now. Wait a bit and retry
                    await Task.Delay(_environmentSettings.ConnectionTimeout * 2);
                    continue;
                }

                //All send tasks ran to completion
                return;
            }
        }

        protected async Task SendNotificationAsync(MonitorBase monitor, StatusHistoryRecord? previous, StatusHistoryRecord current)
        {
            //Check one last time to see if we are still leader
            if (!_clusterService.IsLocalLeader()) return;

            var dbgMessage = $"Sending status change notifications for monitor({monitor.Name}|{monitor.Id}).";

            if (previous != null) dbgMessage += $" Previous status '{Enum.GetName(previous.Status)}' since {previous.FromUtc}.";

            dbgMessage += $" New status '{Enum.GetName(current.Status)}' from {current.FromUtc}.";

            _logger.LogDebug(dbgMessage);

            //Send the notification through all of the linked providers
            foreach (var notificationProvider in monitor.NotificationProviders.Where(x => x.Enabled))
            {
                INotificationSender sender = notificationProvider switch
                {
                    SmtpEmailProvider smtpEmailProvider => new SmtpEmailSender(smtpEmailProvider),
                    WebhookProvider webhookProvider => new WebhookSender(webhookProvider),
                    _ => throw new NotImplementedException()
                };

                await sender.SendNotificationAsync(monitor, previous, current);
            }

            await _clusterService.ReplicateAsync(new CreateNotificationHistoryRecordCmd
            {
                MonitorId = monitor.Id,
                StatusUtc = current.FromUtc
            });
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
