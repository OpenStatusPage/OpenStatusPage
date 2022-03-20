using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.Coordination.Commands;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Shared.Interfaces;
using System.Collections.Concurrent;

namespace OpenStatusPage.Server.Application.Monitoring.Worker
{
    public class WorkerService : ISingletonService
    {
        private readonly ILogger<WorkerService> _logger;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly ClusterService _clusterService;
        private readonly ClusterMember _localWorker;

        protected List<TaskAssignmentCmd> AssignmentBuffer { get; set; } = new();

        protected ConcurrentDictionary<string, MonitorTask> MonitoringTasks { get; set; } = new();

        protected List<TaskAssignmentCmd> ActiveTaskAssignments { get; set; } = new();

        public WorkerService(ClusterService clusterService, ScopedMediatorExecutor scopedMediator, ILogger<WorkerService> logger)
        {
            _logger = logger;
            _scopedMediator = scopedMediator;
            _clusterService = clusterService;
            _clusterService.OnOperational += (sender, args) => OnOperational();
            _localWorker = _clusterService.GetLocalMember();
        }

        public async Task HandleTaskAssignmentAsync(TaskAssignmentCmd taskAssignment, CancellationToken cancellationToken = default)
        {
            if (_clusterService.IsOperational)
            {
                await StartTaskAssignmentAsync(taskAssignment, cancellationToken);
            }
            else
            {
                //Store the assignments until the cluster member is operational
                AssignmentBuffer.Add(taskAssignment);
            }
        }

        public async Task StopMonitorAsync(string monitorId, CancellationToken cancellationToken = default)
        {
            ActiveTaskAssignments.RemoveAll(x => x.MonitorId == monitorId);

            AssignmentBuffer.RemoveAll(x => x.MonitorId == monitorId);

            var stopAssignments = MonitoringTasks.Keys
                .Where(x => x.StartsWith(monitorId))
                .ToList();

            var cancelTasks = new List<Task>();

            foreach (var taskAssignment in stopAssignments)
            {
                if (MonitoringTasks.Remove(taskAssignment, out var monitorTask))
                {
                    cancelTasks.Add(monitorTask.CancelAsync());
                }
            }

            try
            {
                await Task.WhenAll(cancelTasks).WaitAsync(cancellationToken);
            }
            catch
            {
            }
        }

        public async Task CancelStaleMonitoringTasksAsync(string monitorId, string? latestDecisionAssignmentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(latestDecisionAssignmentId)) return;

            var latestDecisionAssignment = ActiveTaskAssignments.FirstOrDefault(x => x.Id == latestDecisionAssignmentId);

            if (latestDecisionAssignment == null)
            {
                //An assignment group made a decision that the local worker is no longer part of
                //Stop all monitoring taks
                await StopMonitorAsync(monitorId, cancellationToken);
            }
            else
            {
                //We are still assigned but we could be running old monitor versions from a transition
                var staleAssignments = ActiveTaskAssignments
                    .Where(x => x.MonitorId == latestDecisionAssignment.MonitorId &&
                                x.DateTime < latestDecisionAssignment.DateTime)
                    .ToList();

                var cancelTasks = new List<Task>();

                foreach (var stale in staleAssignments)
                {
                    //Stop any monitoring tasks that are outdated
                    if (stale.MonitorVersion < latestDecisionAssignment.MonitorVersion &&
                        MonitoringTasks.TryGetValue($"{stale.MonitorId}:{stale.MonitorVersion}", out var monitorTask))
                    {
                        cancelTasks.Add(monitorTask.CancelAsync());
                    }

                    //Remove them from the list
                    ActiveTaskAssignments.Remove(stale);
                }

                try
                {
                    await Task.WhenAll(cancelTasks).WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        protected void OnOperational()
        {
            Task.Run(() =>
            {
                //Keep only the most recent assignments per monitor
                var assignments = AssignmentBuffer
                    .GroupBy(x => x.MonitorId)
                    .Select(group => group
                        .OrderByDescending(assignment => assignment.DateTime)
                        .First())
                    .ToList();

                AssignmentBuffer.Clear();

                assignments.ForEach(async assignment => await StartTaskAssignmentAsync(assignment));
            });
        }

        protected async Task StartTaskAssignmentAsync(TaskAssignmentCmd taskAssignment, CancellationToken cancellationToken = default)
        {
            //See if the task assignment contains our local worker, if not, ignore
            if (!taskAssignment.WorkerIds.Contains(_localWorker.Id)) return;

            ActiveTaskAssignments.Add(taskAssignment);

            var key = $"{taskAssignment.MonitorId}:{taskAssignment.MonitorVersion}";

            //See if this monitor version is already being checked
            if (MonitoringTasks.ContainsKey(key)) return;

            //Check one last time before starting the thread if the operation was cancelled
            if (cancellationToken.IsCancellationRequested) return;

            //Start the worker task
            MonitoringTasks[key] = new(taskAssignment.MonitorId, taskAssignment.MonitorVersion, _logger, _scopedMediator);
        }
    }
}
