using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.Coordination.Commands;
using OpenStatusPage.Server.Application.Monitors;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Interfaces;
using OpenStatusPage.Shared.Utilities;

namespace OpenStatusPage.Server.Application.Monitoring.Coordination
{
    public class TaskCoordinationService : ISingletonService, IHostedService
    {
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly ClusterService _clusterService;
        private readonly ILogger<TaskCoordinationService> _logger;
        private readonly EnvironmentSettings _environmentSettings;

        protected Debouncer Debouncer { get; set; } = new();

        protected List<TaskAssignmentCmd> TaskAssignments { get; set; } = new();

        public TaskCoordinationService(ScopedMediatorExecutor scopedMediator,
                                       ClusterService clusterService,
                                       ILogger<TaskCoordinationService> logger,
                                       EnvironmentSettings environmentSettings)
        {
            _scopedMediator = scopedMediator;
            _clusterService = clusterService;
            _logger = logger;
            _environmentSettings = environmentSettings;
            _clusterService.OnClusterLeaderChanged += (sender, args) => TriggerRedistributionNow();
            _clusterService.OnMemberAdded += (sender, args) => DebouncedRedistribution();
            _clusterService.OnMemberRemoved += (sender, args) => TriggerRedistributionNow();
            _clusterService.OnMemberStatusChanged += (sender, args) => DebouncedRedistribution();
        }

        public void DebouncedRedistribution(TimeSpan? debounce = default)
        {
            //Only the leader handles task distribution
            if (!_clusterService.IsLocalLeader()) return;

            //Wait for twice the connection timeout, to give cluster changes the chance to go through and reset the debounce
            Debouncer.Debounce(debounce ?? TimeSpan.FromMilliseconds(_environmentSettings.ConnectionTimeout * 2), RedistributeTasksAsync);
        }

        public void TriggerRedistributionNow()
        {
            DebouncedRedistribution(TimeSpan.Zero);
        }

        public async Task ApplyTaskAssignmentAsync(TaskAssignmentCmd taskAssignment, CancellationToken cancellationToken = default)
        {
            if (!TaskAssignments.Any(x => x.Id == taskAssignment.Id)) TaskAssignments.Add(taskAssignment);
        }

        public async Task<List<TaskAssignmentCmd>> GetTaskAssignmentsAsync(CancellationToken cancellationToken = default)
        {
            return TaskAssignments.ToList();
        }

        public async Task RemoveDeletedMonitorAssignmentsAsync(string monitorId, CancellationToken cancellationToken)
        {
            TaskAssignments.RemoveAll(x => x.MonitorId == monitorId);
        }

        public async Task RemoveStaleAssignmentsAsync(string monitorId, string? taskAssignmentId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(taskAssignmentId)) return;

            var latestDecisonAssignment = TaskAssignments.FirstOrDefault(x => x.Id == taskAssignmentId);

            if (latestDecisonAssignment == null) return;

            //Remove all assignments for the monitor that are older than the latest one that made a decision, because those will no longer be considered for anything
            TaskAssignments.RemoveAll(x => x.MonitorId == monitorId && x.DateTime < latestDecisonAssignment.DateTime);
        }

        protected async Task RedistributeTasksAsync()
        {
            //Only the leader handles task distribution
            if (!_clusterService.IsLocalLeader()) return;

            var enabledMonitors = (await _scopedMediator.Send(new MonitorsQuery
            {
                Query = new(query => query.Where(x => x.Enabled))
            }))?.Monitors;

            //No monitors to run checks on
            if (enabledMonitors == null || enabledMonitors.Count == 0) return;

            var workers = (await _clusterService.GetMembersAsync(true))
                .Where(x => x.Availability == ClusterMemberAvailability.Available)
                .Where(x => x.AvgCpuLoad.HasValue)
                .ToList();

            //No valid workers to give tasks to
            if (workers.Count == 0)
            {
                //Re-attempt distrubtion in 30 seconds. Once we are not leader anymore, we do not reach this code
                DebouncedRedistribution(TimeSpan.FromSeconds(30));

                //Abort current attempt
                return;
            }

            //Create a working copy of the current assignments
            var workingCopyAssignments = TaskAssignments.ToList();

            //Store new assinments made to replicate them at the end
            var newAssingments = new List<TaskAssignmentCmd>();

            //Store any un-assignments to make sure they are included in the replication
            var unAssignments = new List<TaskAssignmentCmd>();

            //Get an estimate ratio of how much cpu % is caused by a running monitor task on the workers
            var taskToCpuRatios = GetWorkerTaskCpuRatio(workers);

            //Calculate the cpu% threshold above which re-balancing will take place
            var cpuThreshold = CalculateCpuThreshold(workers);

            //Keeep track of how many tasks are assigned to workers in case their cpu load is the same, that can be used for sorting.
            var assignmentsCountPerWorker = GetInitalAssignmentCounts(workers, workingCopyAssignments);

            //Keep track of estimated cpu load per worker during changes
            var estimatedCpuLoad = GetInitalWorkerCpuLoadEstimates(workers);

            //Remove assignments from workers with higher than median cpu usage to try and re-balance the workload across all available members
            foreach (var worker in workers)
            {
                //Workers that are not over the median cpu usage, remain untouched
                if (worker.AvgCpuLoad!.Value <= cpuThreshold) continue;

                //No cpu% per task ratio known, so no way of telling how to reduce it
                if (!taskToCpuRatios.TryGetValue(worker, out var taskCpuRatio)) continue;

                //Get the latest assignment where this worker is used
                //Order them by date to be able to take the "oldest" assignments that are the most recent for the monitor
                var sortedAssignments = workingCopyAssignments
                    .Where(x => x.WorkerIds.Contains(worker.Id))
                    .GroupBy(x => x.MonitorId)
                    .Select(group => group
                        .OrderByDescending(assignment => assignment.DateTime)
                        .First())
                    .OrderBy(x => x.DateTime);

                //Find out how many to remove, to get the worker close to the median
                var cpuOverload = worker.AvgCpuLoad.Value - cpuThreshold;
                var overloadTasks = (int)(cpuOverload / taskCpuRatio); //e.g. 3.7% overload, each task is estimated to cause 1% load, remove 4 of them

                //Un-assign the monitor by creating a new assignment without him
                foreach (var removeFromAssignment in sortedAssignments.Take(overloadTasks))
                {
                    var remainingWorkers = removeFromAssignment.WorkerIds.ToHashSet();
                    remainingWorkers.Remove(worker.Id);

                    //Ensure that the new assignment has a newer timestamp than the old one, in case this runs really fast and utcnow remains the same between two un-assignements
                    var now = DateTimeOffset.UtcNow;
                    var creationTime = now > removeFromAssignment.DateTime ? now : now.AddTicks(1);

                    var unAssignment = new TaskAssignmentCmd()
                    {
                        Id = Guid.NewGuid().ToString(),
                        DateTime = creationTime,
                        MonitorId = removeFromAssignment.MonitorId,
                        MonitorVersion = removeFromAssignment.MonitorVersion,
                        WorkerIds = remainingWorkers,
                    };

                    unAssignments.Add(unAssignment);
                    workingCopyAssignments.Add(unAssignment);
                }

                //Reduce the assigned task count
                assignmentsCountPerWorker[worker] -= overloadTasks;

                //Reduced the estimated load
                estimatedCpuLoad[worker] -= overloadTasks * taskCpuRatio;
            }

            //Assign all missing monitors
            foreach (var monitor in enabledMonitors)
            {
                //Take the last created assignment for this monitor if there are any
                var existingAssignment = workingCopyAssignments
                    .Where(x => x.MonitorId == monitor.Id && x.MonitorVersion == monitor.Version)
                    .OrderBy(x => x.DateTime)
                    .LastOrDefault();

                //If the monitor has tags assigned, only accept workers that have it as well
                var requiredTags = monitor.GetTags();
                var qualifiedWorkers = requiredTags.Count == 0 ? workers :
                    workers.Where(worker => worker.Tags.Intersect(requiredTags).Count() == requiredTags.Count)
                    .ToList();

                //This monitor already has an assignment with the exact worker count requested, and all the workers are still in the qualified pool
                if (existingAssignment != null &&
                    existingAssignment.WorkerIds.Count == monitor.WorkerCount &&
                    existingAssignment.WorkerIds.All(workerId => qualifiedWorkers.Any(y => y.Id == workerId))) continue;

                //Sort the workers by their current workload estimate (ASC order - so low cpu% first, then if same cpu%, balance the task count)
                var sortedWorkers = qualifiedWorkers
                    .OrderBy(worker => estimatedCpuLoad[worker])
                    .ThenBy(worker => assignmentsCountPerWorker[worker])
                    .ToList();

                List<ClusterMember> selectedWorkers;

                if (existingAssignment == null) //There is no existing assignment
                {
                    //Select as many workers as the monitor requested if possible
                    selectedWorkers = sortedWorkers.Take(monitor.WorkerCount).ToList();

                    //Increase the task count and estimate cpu usage for the selected workers
                    selectedWorkers.ForEach(worker =>
                    {
                        assignmentsCountPerWorker[worker] += 1;

                        estimatedCpuLoad[worker] += taskToCpuRatios[worker];
                    });
                }
                else //We have workers assigned but need to add more or remove some
                {
                    //Get the existing qualified members. If they are no longer qualified, they will be missing from the collection
                    var stillQualified = new List<ClusterMember>();
                    var disqualified = new List<ClusterMember>();

                    foreach (var existingWorkerId in existingAssignment.WorkerIds)
                    {
                        var existingWorker = sortedWorkers.FirstOrDefault(x => x.Id == existingWorkerId);

                        if (existingWorker != null)
                        {
                            stillQualified.Add(existingWorker);
                        }
                        else
                        {
                            var worker = workers.FirstOrDefault(x => x.Id == existingWorkerId);

                            //If the worker is still part of the overall pool, bust just not qualified for this task anymore, remember him to remove his workload
                            if (worker != null) disqualified.Add(worker);
                        }
                    }

                    //If we have too few, it will take all existing qualified members
                    //If we have too many, it will only take out as many as we need (and those are sorted by cpu& estimate)
                    var alreadyAssignedWorkers = stillQualified
                        .Take(monitor.WorkerCount)
                        .ToList();

                    //Get the remaining count of workers to match workerCount from the pool of sorted workers, that are are not already assigned
                    var additionalWorkers = sortedWorkers
                        .Where(x => !alreadyAssignedWorkers.Contains(x))
                        .Take(monitor.WorkerCount - alreadyAssignedWorkers.Count)
                        .ToList();

                    //The new group of selected workers are the ones still qualified for the job and any additional workers we might need to add
                    selectedWorkers = alreadyAssignedWorkers.Concat(additionalWorkers).ToList();

                    //Decrease the task count and  cpu estimate for disqualified members, as they are no longer assigned
                    disqualified.ForEach(disqualified =>
                    {
                        assignmentsCountPerWorker[disqualified] = Math.Max(0, assignmentsCountPerWorker[disqualified] - 1);

                        estimatedCpuLoad[disqualified] -= taskToCpuRatios[disqualified];
                    });

                    //Increase the task count and estimate cpu usage for the additional workers
                    additionalWorkers.ForEach(worker =>
                    {
                        assignmentsCountPerWorker[worker] += 1;

                        estimatedCpuLoad[worker] += taskToCpuRatios[worker];
                    });

                    //If the new assignment is identical to the existing one, ignore it.
                    //This can happen if there are too few workers for a monitor and there are still not enough
                    if (existingAssignment.WorkerIds.SetEquals(selectedWorkers.Select(x => x.Id))) continue;
                }

                //No matching workers found for this monitor
                if (selectedWorkers.Count == 0)
                {
                    _logger.LogCritical($"No qualified workers found for Monitor({monitor.Name}|{monitor.Id}|Version {monitor.Version}). The monitor will remain in-active until there are qualified workers available.");
                    continue;
                }

                //Ensure that the new assignment will be the latest one (by DateTime)
                var now = DateTimeOffset.UtcNow;
                var creationTime = existingAssignment == null || now > existingAssignment.DateTime ? now : now.AddTicks(1);

                var newAssignment = new TaskAssignmentCmd()
                {
                    Id = Guid.NewGuid().ToString(),
                    DateTime = creationTime,
                    MonitorId = monitor.Id,
                    MonitorVersion = monitor.Version,
                    WorkerIds = new(selectedWorkers.Select(x => x.Id)),
                };

                newAssingments.Add(newAssignment);
            }

            //Make sure that if new monitor assignments were made, we send the unassignments
            //This covers the case for when a monitor wanted less workers, but some workers got their assignment removed already due to load balancing
            foreach (var unAssignment in unAssignments)
            {
                if (!newAssingments.Any(x => x.MonitorId == unAssignment.MonitorId))
                {
                    newAssingments.Add(unAssignment);
                }
            }

            //Replicate new assignments
            foreach (var taskAssignment in newAssingments)
            {
                await _clusterService.ReplicateAsync(taskAssignment);
            }

            var assingmentString = $"------ NEW WORKER ASSIGNMENTS ({newAssingments.Count}) ------";

            foreach (var taskAssignment in newAssingments.OrderBy(x => x.MonitorId))
            {
                var monitor = enabledMonitors.First(x => x.Id == taskAssignment.MonitorId);
                var selectedWorkers = taskAssignment.WorkerIds.Select(x => workers.First(y => y.Id == x).Endpoint).ToList();

                assingmentString += $"\nMonitor({monitor.Name}|{monitor.Id}|Version {monitor.Version}) is now being worked on by [{string.Join('|', selectedWorkers)}]";
            }

            assingmentString += "\n";

            foreach (var worker in workers)
            {
                var assignments = workingCopyAssignments
                    .Concat(newAssingments)
                    .GroupBy(x => x.MonitorId)
                    .Select(group => group
                        .OrderByDescending(assignment => assignment.DateTime)
                        .First())
                    .Where(x => x.WorkerIds.Contains(worker.Id))
                    .Where(x => enabledMonitors.Any(monitor => monitor.Id == x.MonitorId)) //Do not count stale assignments.
                    .ToList();

                assingmentString += $"\nWorker({worker.Id}|{worker.Endpoint}) has {assignments.Count} tasks assigned. Tags({string.Join(';', worker.Tags)}). CPU Load estimate is {estimatedCpuLoad[worker]:0.00}%";

                foreach (var taskAssignment in assignments)
                {
                    var monitor = enabledMonitors.First(x => x.Id == taskAssignment.MonitorId);
                    assingmentString += $"\n\t- Monitor({monitor.Name}|{monitor.Id}|Version {monitor.Version}) WorkerCount:{monitor.WorkerCount} Tags({monitor.Tags})";
                }
            }

            _logger.LogDebug(assingmentString);
        }

        protected Dictionary<ClusterMember, double> GetWorkerTaskCpuRatio(IReadOnlyCollection<ClusterMember> workers)
        {
            var workerTaskCpuRatio = new Dictionary<ClusterMember, double>();

            foreach (var worker in workers)
            {
                var assignments = TaskAssignments.Where(x => x.WorkerIds.Contains(worker.Id)).Count();

                //Assume over agressive value of 1% per task to not overload a weak worker until we have proper performace metrics based on earlier task executions
                workerTaskCpuRatio[worker] = assignments > 0 ? worker.AvgCpuLoad!.Value / assignments : 1.0;
            }

            return workerTaskCpuRatio;
        }

        protected static Dictionary<ClusterMember, double> GetInitalAssignmentCounts(List<ClusterMember> workers, List<TaskAssignmentCmd> taskAssignments)
        {
            var assignmentCounts = new Dictionary<ClusterMember, double>();

            workers.ForEach(worker => assignmentCounts[worker] = taskAssignments
                    .GroupBy(x => x.MonitorId)
                    .Select(group => group
                        .OrderByDescending(assignment => assignment.DateTime)
                        .First())
                    .Where(x => x.WorkerIds.Contains(worker.Id))
                    .Count());

            return assignmentCounts;
        }

        protected static Dictionary<ClusterMember, double> GetInitalWorkerCpuLoadEstimates(List<ClusterMember> workers)
        {
            var estimatedCpuLoad = new Dictionary<ClusterMember, double>();

            workers.ForEach(x => estimatedCpuLoad[x] = x.AvgCpuLoad!.Value);

            return estimatedCpuLoad;
        }

        protected static double CalculateCpuThreshold(IReadOnlyCollection<ClusterMember> workers)
        {
            return workers.Select(x => x.AvgCpuLoad!.Value).Average();
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
