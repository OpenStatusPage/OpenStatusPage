using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Configuration.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.Coordination.Commands;
using OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands;
using OpenStatusPage.Server.Application.Monitoring.Worker.Commands;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Interfaces;
using OpenStatusPage.Shared.Utilities;
using System.Collections.Concurrent;
using static OpenStatusPage.Server.Application.Monitoring.StatusTimeline.StatusTimelineService.Timeline;

namespace OpenStatusPage.Server.Application.Monitoring.StatusTimeline
{
    public class StatusTimelineService : ISingletonService, IHostedService
    {
        private readonly ILogger<StatusTimelineService> _logger;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly ClusterService _clusterService;
        private readonly EnvironmentSettings _environmentSettings;

        protected ConcurrentDictionary<string, Timeline> LocalWorkerBuffer { get; set; } = new();

        protected ConcurrentDictionary<string, Task> SendToLeaderTasks { get; set; } = new();

        protected ConcurrentDictionary<string, TimeSpan> MonitorIntervals { get; set; } = new();

        protected ConcurrentDictionary<string, Lazy<Task>> StatusDeterminationTasks { get; set; } = new();

        protected ConcurrentDictionary<string, Timeline> SynchronizedTimeline { get; set; } = new();

        protected Debouncer StatusFlushDebouncer { get; set; } = new();

        public StatusTimelineService(ILogger<StatusTimelineService> logger,
                                     ScopedMediatorExecutor scopedMediator,
                                     ClusterService clusterService,
                                     EnvironmentSettings environmentSettings)
        {
            _logger = logger;
            _scopedMediator = scopedMediator;
            _clusterService = clusterService;
            _environmentSettings = environmentSettings;

            //Subscribe to cluster events that indicate potential issues (such as leader change, member gone) to flush sync the current status to everyone still available
            _clusterService.OnClusterLeaderChanged += (sender, args) => TriggerStatusFlush();
            _clusterService.OnMemberRemoved += (sender, args) => TriggerStatusFlush();
            _clusterService.OnMemberStatusChanged += (sender, args) =>
            {
                if (args.NewAvailability != ClusterMemberAvailability.Available)
                {
                    TriggerStatusFlush();
                }
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var monitors = (await _scopedMediator.Send(new MonitorsQuery(), cancellationToken))?.Monitors;

            if (monitors == null) return;

            foreach (var monitor in monitors)
            {
                await AddMonitorIntervalAsync(monitor.Id, monitor.Version, monitor.Interval, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task AddMonitorIntervalAsync(string monitorId, long monitorVersion, TimeSpan interval, CancellationToken cancellationToken = default)
        {
            if (MonitorIntervals.TryAdd($"{monitorId}:{monitorVersion}", interval))
            {
                _logger.LogDebug($"Added monitor({monitorId}|Version {monitorVersion}) with interval of '{interval}'.");
            }
        }

        protected async Task<TimeSpan?> GetMonitorIntervalAsync(string monitorId, long monitorVersion, CancellationToken cancellationToken = default)
        {
            if (MonitorIntervals.TryGetValue($"{monitorId}:{monitorVersion}", out var result)) return result;

            return null;
        }

        public async Task AddLocalServiceStatusResultAsync(string monitorId, long monitorVersion, DateTimeOffset dateTime, ServiceStatus serviceStatus, CancellationToken cancellationToken = default)
        {
            var buffer = await GetLocalBufferAsync(monitorId, monitorVersion, true, cancellationToken);

            //Value is outdated on arrival (maybe because of force sync from leader timeline) so we ignore it.
            if (buffer.Until.HasValue && dateTime <= buffer.Until.Value) return;

            //Ensure that the write thread has exclusvie locking access
            await buffer.Access.WaitAsync(cancellationToken);

            var statusChanged = buffer.LatestStatus != serviceStatus;

            var baseMsg = $"LOCAL Worker status for monitor({monitorId}|Version {monitorVersion}) was determined at {dateTime.DateTime} to";

            if (statusChanged)
            {
                var from = buffer.LastOrDefault()?.From;
                _logger.LogDebug($"{baseMsg} have changed from '{Enum.GetName(buffer.LatestStatus)}' {(from.HasValue ? $"since {from.Value.DateTime} " : "")}to '{Enum.GetName(serviceStatus)}'.");
            }
            else
            {
                _logger.LogDebug($"{baseMsg} remain '{Enum.GetName(serviceStatus)}' since {buffer.LastOrDefault()?.From.DateTime ?? dateTime.DateTime}.");
            }

            if (buffer.Count == 0 || statusChanged)
            {
                //No timeline data recorded yet or status has changed
                buffer.AddLast(new Segment()
                {
                    From = dateTime,
                    ServiceStatus = serviceStatus
                });
            }

            //Update timeline with new valid until info
            buffer.Until = dateTime;

            //Release write access so waiting threads can read
            buffer.Access.Release();

            //Trigger sync to leader mechanism on status change detection 
            if (statusChanged) TriggerSendToLeader(monitorId, monitorVersion, cancellationToken);
        }

        public async Task HandleServiceStatusReportAsync(string monitorId, long monitorVersion, DateTimeOffset dateTime, ServiceStatus serviceStatus, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"HandleServiceStatusReportAsync({monitorId}, {monitorVersion}, {dateTime.DateTime}, {serviceStatus})");

            //Make sure we are still the leader and in operational state when this request arrives, or else just ignore it
            if (!_clusterService.IsLocalLeader() || !_clusterService.IsOperational) return;

            //Trigger the time line sync for up to (including) the reported date time
            await DetermineStatusAsync(monitorId, dateTime, cancellationToken);
        }

        protected async Task DetermineStatusAsync(string monitorId, DateTimeOffset dateTime, CancellationToken cancellationToken = default)
        {
            //Set a new runner if there is none already and await its completion
            await StatusDeterminationTasks.GetOrAdd($"{monitorId}:{dateTime}", (key) => new(Task.Run(GetAndProcessWorkerTimelines, cancellationToken))).Value;

            //Recursive function to handle the specified date time and all the data that came before it
            async Task GetAndProcessWorkerTimelines()
            {
                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) Started.");

                //Get or create synced timeline
                var syncTimeline = await GetSynchronizedTimelineAsync(monitorId, true, cancellationToken);

                //Timeline already has confirmed data up to (including) the report date time, so this request is ignored
                if (syncTimeline.Count > 0 && dateTime <= syncTimeline.Until) return;

                //Get task assigments for the monitor
                var assignments = (await _scopedMediator.Send(new TaskAssignmentsQuery(), cancellationToken))?.TaskAssignments.Where(x => x.MonitorId == monitorId).ToList();

                //If no assignment data is known, we can not ask the workers for data, hence it is pointles to process this further
                if (assignments == null)
                {
                    _logger.LogError($"Could not determine status for monitor({monitorId}) at {dateTime.DateTime}. No worker assingments found to ask for data.");
                    return;
                }

                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 Preprocessing.");

                //Check if any of the active assignments has something to report before this current date time (to avoid gaps due to workers racing to report results for different times)
                var gapCheckTasks = new List<Task<DateTimeOffset?>>();

                foreach (var workerId in assignments.SelectMany(x => x.WorkerIds).Distinct())
                {
                    gapCheckTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var member = await _clusterService.GetMemberByIdAsync(workerId);

                            //If the member has since left the cluster or is not available anymore, we have to assume his data is lost
                            if (member == null || member.Availability != ClusterMemberAvailability.Available) return null;

                            _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 Asking for pending changes from {member.Endpoint}");

                            //Ask the worker if he knows about any status changes before the current date time being processed
                            var result = await _clusterService.SendAsync(member, new FetchPendingChangesCmd()
                            {
                                MonitorId = monitorId,
                                Before = dateTime
                            }, cancellationToken);

                            if (result.HasValue)
                            {
                                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 Pending change result from {member.Endpoint}: {result.Value.DateTime}");

                                return result;
                            }

                            return result;
                        }
                        catch
                        {
                        }

                        return null;
                    }));
                }

                await Task.WhenAll(gapCheckTasks);

                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 Pending change results fetched.");

                var precedingChange = gapCheckTasks
                    .Select(task => task.Result)
                    .Where(x => x.HasValue)
                    .Min();

                if (precedingChange.HasValue)
                {
                    _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 Preceding change found for {precedingChange.Value}. Processing that first.");

                    await DetermineStatusAsync(monitorId, precedingChange.Value, cancellationToken);
                }
                else
                {
                    _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #1 No Preceding changes found. Continue to processing {dateTime.DateTime}");
                }

                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #2 Preprocessing over.");

                var reportStatus = ServiceStatus.Unknown;

                //Start the status processing by trying to ask the last assigned group for their decision
                var currentAssignment = assignments.MaxBy(x => x.DateTime);

                while (
                    !cancellationToken.IsCancellationRequested && //Not aborted
                    currentAssignment != null && //There are still groups of workers to ask
                    _clusterService.IsLocalLeader()) //Still in leader role to process the data
                {
                    try
                    {
                        _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #3 Fetching from workers {string.Join(", ", currentAssignment.WorkerIds)}");

                        //Try and get the status from each of the assigned workers
                        var fetchStatusTasks = new List<Task<ServiceStatus?>>();

                        foreach (var workerId in currentAssignment.WorkerIds)
                        {
                            fetchStatusTasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    while (!cancellationToken.IsCancellationRequested)
                                    {
                                        var member = await _clusterService.GetMemberByIdAsync(workerId);

                                        //If the member has since left the cluster or is not available anymore, we have to assume his data is lost
                                        if (member == null || member.Availability != ClusterMemberAvailability.Available) return null;

                                        _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #4 Fetching from {member.Endpoint}");

                                        var result = await _clusterService.SendAsync(member, new ServiceStatusQuery()
                                        {
                                            MonitorId = currentAssignment.MonitorId,
                                            MonitorVersion = currentAssignment.MonitorVersion,
                                            At = dateTime
                                        }, cancellationToken);

                                        //He had data for the requested time
                                        if (result.HasValue)
                                        {
                                            _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #4 Result from {member.Endpoint}: {Enum.GetName(result.Value)}");

                                            return result;
                                        }

                                        _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #4 Retrying...");

                                        //As long as the member did not have the data we asked him for, try againt a bit later
                                        await Task.Delay(_environmentSettings.ConnectionTimeout);
                                    }
                                }
                                catch (Exception ex)
                                {
                                }

                                return null;
                            }));
                        }

                        await Task.WhenAll(fetchStatusTasks);

                        _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #5 Results fetched.");

                        var allResults = fetchStatusTasks.Select(task => task.Result).ToList();
                        var validResults = allResults.Where(result => result != null && result.Value != ServiceStatus.Unknown).ToList();

                        //We need a majority of valid results to decide the final
                        if (validResults.Count > allResults.Count / 2)
                        {
                            if (validResults.All(x => x.HasValue && x.Value == ServiceStatus.Unavailable))
                            {
                                //Report a full outage if all members agree
                                reportStatus = ServiceStatus.Unavailable;
                            }
                            else if (validResults.Any(x => x.HasValue && x.Value != ServiceStatus.Available))
                            {
                                //Report partial outage if any of the worker reports something other than available
                                reportStatus = ServiceStatus.Degraded;
                            }
                            else
                            {
                                //Otherwise available. Unknown can only be a minority in the results considered
                                reportStatus = ServiceStatus.Available;
                            }

                            //Decision was made, break out of the loop
                            break;
                        }
                        else
                        {
                            _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #6 Not enough valid worker results. Trying to ask previously assigned group.");
                        }
                    }
                    catch
                    {
                    }

                    //See if there is another worker group to ask that was previously handling the task, as they might be able to still answer for this date time
                    currentAssignment = assignments.LastOrDefault(x => x.DateTime < currentAssignment.DateTime);
                }

                _logger.LogDebug($"GetAndProcessWorkerTimelines({monitorId}, {dateTime.DateTime}) #7 Replicating final status: {Enum.GetName(reportStatus)}.");

                //Replicate final decision
                await _clusterService.ReplicateAsync(new UpdateServiceStatusCmd()
                {
                    MonitorId = monitorId,
                    DateTime = dateTime,
                    ServiceStatus = reportStatus,
                    TaskAssignmentId = currentAssignment?.Id
                }, cancellationToken);
            }
        }

        public async Task HandleServiceStatusUpdateAsync(string monitorId, DateTimeOffset dateTime, ServiceStatus serviceStatus, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"HandleServiceStatusUpdateAsync({monitorId}, {dateTime.DateTime}, {serviceStatus})");

            var syncTimeline = await GetSynchronizedTimelineAsync(monitorId, true, cancellationToken);

            //We recieved data that is already included in the local synced timeline, ignore ...
            if (syncTimeline.Until.HasValue && dateTime <= syncTimeline.Until.Value) return;

            //Ensure that the write thread has exclusvie locking access
            await syncTimeline.Access.WaitAsync(cancellationToken);

            var from = syncTimeline.LastOrDefault()?.From;
            var lastStatus = syncTimeline.LatestStatus;

            var statusChanged = lastStatus != serviceStatus;

            var baseMsg = $"Status for monitor({monitorId}) was determined at {dateTime.DateTime} to";

            if (statusChanged)
            {

                _logger.LogInformation($"{baseMsg} have changed from '{Enum.GetName(syncTimeline.LatestStatus)}' {(from.HasValue ? $"since {from.Value.DateTime} " : "")}to '{Enum.GetName(serviceStatus)}'.");
            }
            else
            {
                _logger.LogInformation($"{baseMsg} remain '{Enum.GetName(serviceStatus)}' since {from?.DateTime ?? dateTime.DateTime}.");
            }

            if (syncTimeline.Count == 0 || statusChanged)
            {
                //No timeline data recorded yet or status has changed
                syncTimeline.AddLast(new Segment()
                {
                    From = dateTime,
                    ServiceStatus = serviceStatus
                });
            }

            //Update timeline with new valid until info
            syncTimeline.Until = dateTime;

            //Reduce memeory usage over time
            if (syncTimeline.Count > 1)
            {
                var appsettings = (await _scopedMediator.Send(new ApplicationSettingsQuery(), cancellationToken))?.ApplicationSettings;

                if (appsettings != null)
                {
                    var removeBefore = DateTimeOffset.UtcNow.AddDays(-appsettings.DaysMonitorHistory);

                    //Remove all status records earlier the history limit, unless they are the latest record we have
                    syncTimeline
                        .Where(x => x.From < removeBefore && x != syncTimeline.Last.Value)
                        .ToList()
                        .ForEach(x => syncTimeline.Remove(x));
                }
            }

            //Release write access so waiting threads can read
            syncTimeline.Access.Release();

            //Collect local buffers to update
            var relevantTimelineKeys = LocalWorkerBuffer.Keys
                .Where(x => x.StartsWith(monitorId));

            foreach (var key in relevantTimelineKeys)
            {
                //Get buffer
                var workerTimeline = LocalWorkerBuffer[key];

                //Ensure that the write thread has exclusvie locking access
                await workerTimeline.Access.WaitAsync(cancellationToken);

                //Find out if we create or update an existing segment
                var affectedSegment = workerTimeline.LastOrDefault(x => dateTime >= x.From);

                if (affectedSegment == null)
                {
                    //No segment found, so we need to add it - this can happen when a sync comes before the local worker completion
                    workerTimeline.AddLast(new Segment()
                    {
                        From = dateTime,
                        ServiceStatus = serviceStatus
                    });

                    workerTimeline.Until = syncTimeline.Until;
                }
                else
                {
                    //Affected segment is not the latest one (because the worker is already ahead)
                    if (affectedSegment != workerTimeline.Last.Value)
                    {
                        //Find the node in the list
                        var listNode = workerTimeline.Find(affectedSegment);

                        //Get the next execution time and see if the next recorde timeline change is after what we would add
                        if (MonitorIntervals.TryGetValue(key, out var result) && affectedSegment.From.Add(result) < listNode.Next.Value.From)
                        {
                            //If so, add a timeline segment so that the worker can detect he has a change since then compared to the sync timeline
                            workerTimeline.AddAfter(listNode, new Segment()
                            {
                                From = affectedSegment.From.Add(result),
                                ServiceStatus = affectedSegment.ServiceStatus
                            });
                        }
                    }

                    //In place data override with synced timeline data
                    affectedSegment.From = dateTime;
                    affectedSegment.ServiceStatus = serviceStatus;
                }

                //Remove all entries that are covered by the synced timeline
                workerTimeline
                    .Where(x => x.From < dateTime)
                    .ToList()
                    .ForEach(x => workerTimeline.Remove(x));

                //Release write access so waiting threads can read
                workerTimeline.Access.Release();
            }

            //Triger local processing for the status history (db, notifications, etc.)
            await _scopedMediator.Send(new CreateStatusHistoryRecordCmd()
            {
                MonitorId = monitorId,
                UtcFrom = dateTime.UtcDateTime,
                Status = serviceStatus
            }, cancellationToken);
        }

        public async Task<ServiceStatus?> GetServiceStatusAsync(string monitorId, long monitorVersion, DateTimeOffset? at, CancellationToken cancellationToken = default)
        {
            //Ask for current status now if nothing was specified
            at ??= DateTimeOffset.UtcNow;

            //Check if data should or will be available for the queried monitor
            var monitorTaskStatus = await _scopedMediator.Send(new MonitorTaskStatusQuery()
            {
                MonitorId = monitorId,
                MonitorVersion = monitorVersion
            }, cancellationToken);

            //Monitoring task is not known, so there are no results and will never be.
            if (monitorTaskStatus == null) return ServiceStatus.Unknown;

            if (monitorTaskStatus.FirstExecutionDispatched.HasValue)
            {
                //Monitor will never have data for the requested time, because the data collection started later
                if (at < monitorTaskStatus.FirstExecutionDispatched) return ServiceStatus.Unknown;
            }
            else if (monitorTaskStatus.NextScheduledExecution.HasValue)
            {
                //Nothing was dispatched yet, but the next scheduled execution is too late for requested time
                if (at < monitorTaskStatus.NextScheduledExecution) return ServiceStatus.Unknown;
            }
            else if (monitorTaskStatus.Active != true)
            {
                //Monitor has started but stopped already without giving any of the data
                return ServiceStatus.Unknown;
            }

            //Local timeline must always be in sync or ahead of global synced timeline
            var buffer = await GetLocalBufferAsync(monitorId, monitorVersion, false, cancellationToken);

            //No buffer for that information is created yet, try again later.
            if (buffer == null) return null;

            //Ensure nobody else can write while we read the timelines
            await buffer.Access.WaitAsync(cancellationToken);

            //Get the status of the last timeline item that describes the time after the "at" query parameter.
            var status = buffer.LastOrDefault(x => at >= x.From)?.ServiceStatus;

            //Release access again
            buffer.Access.Release();

            return status;
        }

        protected async Task<Timeline?> GetLocalBufferAsync(string monitorId, long monitorVersion, bool create = false, CancellationToken cancellationToken = default)
        {
            var key = $"{monitorId}:{monitorVersion}";

            var localTimeline = LocalWorkerBuffer.GetValueOrDefault(key);

            if (localTimeline == null && create)
            {
                localTimeline = new Timeline();

                //Add synced timeline infos if available
                var syncTimeline = await GetSynchronizedTimelineAsync(monitorId, true, cancellationToken);

                if (syncTimeline != null && syncTimeline.Count > 0)
                {
                    //Begin timeline with synced data
                    localTimeline.AddFirst(new Segment()
                    {
                        From = syncTimeline.Until!.Value,
                        ServiceStatus = syncTimeline.LatestStatus
                    });

                    localTimeline.Until = syncTimeline.Until;
                }

                LocalWorkerBuffer.TryAdd(key, localTimeline);
            }

            return localTimeline;
        }

        protected async Task<Timeline?> GetSynchronizedTimelineAsync(string monitorId, bool create = false, CancellationToken cancellationToken = default)
        {
            var value = SynchronizedTimeline.GetValueOrDefault(monitorId);

            if (value == null && create)
            {
                value = new Timeline();

                var latestRecord = await _scopedMediator.Send(new GetStatusFromHistoryCmd()
                {
                    MonitorId = monitorId,
                    UtcAt = DateTime.UtcNow
                }, cancellationToken);

                if (latestRecord != null)
                {
                    value.AddFirst(new Segment()
                    {
                        From = latestRecord.FromUtc,
                        ServiceStatus = latestRecord.Status
                    });

                    value.Until = latestRecord.FromUtc;
                }

                SynchronizedTimeline.TryAdd(monitorId, value);
            }

            return value;
        }

        public async Task<DateTimeOffset?> GetPendingChangeBeforeAsync(string monitorId, DateTimeOffset before, CancellationToken cancellationToken = default)
        {
            var syncedTimeline = await GetSynchronizedTimelineAsync(monitorId, true, cancellationToken);

            var earliestChange = LocalWorkerBuffer
                .Where(buffer => buffer.Key.StartsWith(monitorId)) //Get all local buffers for the monitor in question
                .SelectMany(buffer => buffer.Value) //Select their timelines items as flattended list
                .Where(timelineItem => syncedTimeline.Count == 0 || timelineItem.From > syncedTimeline.Until) //Check all timeline items for a difference to the synced timeline
                .Where(timelineItem => timelineItem.From < before) //Only count changes before the requested date time
                .MinBy(timelineItem => timelineItem.From); //Select the earliest change

            return earliestChange?.From;
        }

        protected void TriggerSendToLeader(string monitorId, long monitorVersion, CancellationToken cancellationToken = default)
        {
            //Set a new runner if if there none for the monitor in that version yet, or the existing one has ended
            lock (SendToLeaderTasks)
            {
                SendToLeaderTasks.AddOrUpdate($"{monitorId}:{monitorVersion}", (key) => Task.Run(SenderTaskAsync), (key, runningTask) =>
                {
                    if (!runningTask.IsCompleted) return runningTask;

                    return Task.Run(SenderTaskAsync);
                });
            }

            async Task SenderTaskAsync()
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var localBuffer = await GetLocalBufferAsync(monitorId, monitorVersion, false, cancellationToken);
                    var syncedTimeline = await GetSynchronizedTimelineAsync(monitorId, false, cancellationToken);

                    if (localBuffer == null) break;

                    //See if there are any changes to send that go beyond the synced timeline
                    var nextUpdate = localBuffer
                        .Where(x => syncedTimeline == null || syncedTimeline.Count == 0 || x.From > syncedTimeline.Until)
                        .OrderBy(x => x.From)
                        .FirstOrDefault();

                    //No updates to send
                    if (nextUpdate == null) break;

                    try
                    {
                        await _clusterService.SendToLeaderAsync(new ReportServiceStatusCmd()
                        {
                            MonitorId = monitorId,
                            MonitorVersion = monitorVersion,
                            DateTime = nextUpdate.From,
                            ServiceStatus = nextUpdate.ServiceStatus
                        }, cancellationToken);
                    }
                    catch
                    {
                    }

                    //Wait for replication or temporary unavailabilities before checking if another reporting round has to take place
                    await Task.Delay(_environmentSettings.ConnectionTimeout, cancellationToken);
                }
            }
        }

        public void TriggerStatusFlush(TimeSpan? afterTime = null)
        {
            //Debounce to wait for more events to trigger
            StatusFlushDebouncer.Debounce(afterTime ?? TimeSpan.FromMilliseconds(_environmentSettings.ConnectionTimeout), FlushStatus);

            async Task FlushStatus()
            {
                //Make sure we are still the leader and in operational state when asked to flush the status of all monitors
                if (!_clusterService.IsLocalLeader() || !_clusterService.IsOperational) return;

                foreach (var monitorData in SynchronizedTimeline)
                {
                    //Do not sync emtpy timelines
                    if (monitorData.Value.Count == 0) continue;

                    try
                    {
                        await _clusterService.ReplicateAsync(new UpdateServiceStatusCmd()
                        {
                            MonitorId = monitorData.Key,
                            DateTime = DateTimeOffset.UtcNow,
                            ServiceStatus = monitorData.Value.LatestStatus
                        });
                    }
                    catch
                    {
                    }
                }

                //Self start flush again after the inital trigger (e.g we were selected leader) to ensure the next flush happens at least in x configured time
                //Outside triggers will cause the flush to happen instantly and after that it goes back to idle here
                var appsettings = (await _scopedMediator.Send(new ApplicationSettingsQuery()))?.ApplicationSettings;

                if (appsettings != null && appsettings.StatusFlushInterval != TimeSpan.Zero)
                {
                    TriggerStatusFlush(appsettings.StatusFlushInterval);
                }
            }
        }

        public async Task RemoveDeletedMonitorDataAsync(string monitorId, CancellationToken cancellationToken = default)
        {
            LocalWorkerBuffer
                .Where(x => x.Key.StartsWith(monitorId))
                .Select(x => x.Key)
                .ToList()
                .ForEach(key => LocalWorkerBuffer.Remove(key, out var _));

            SendToLeaderTasks
                .Where(x => x.Key.StartsWith(monitorId))
                .Select(x => x.Key)
                .ToList()
                .ForEach(key => SendToLeaderTasks.Remove(key, out var _));

            MonitorIntervals
                .Where(x => x.Key.StartsWith(monitorId))
                .Select(x => x.Key)
                .ToList()
                .ForEach(key => MonitorIntervals.Remove(key, out var _));

            StatusDeterminationTasks
                .Where(x => x.Key.StartsWith(monitorId))
                .Select(x => x.Key)
                .ToList()
                .ForEach(key => StatusDeterminationTasks.Remove(key, out var _));

            SynchronizedTimeline.Remove(monitorId, out var _);
        }

        public class Timeline : LinkedList<Segment>
        {
            public SemaphoreSlim Access { get; set; } = new(1);

            public DateTimeOffset? Until { get; set; }

            public class Segment
            {
                public DateTimeOffset From { get; set; }

                public ServiceStatus ServiceStatus { get; set; }
            }

            public ServiceStatus LatestStatus => Last?.Value.ServiceStatus ?? ServiceStatus.Unknown;
        }
    }
}
