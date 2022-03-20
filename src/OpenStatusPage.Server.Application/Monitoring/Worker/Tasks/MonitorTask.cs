using DotNext.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.StatusTimeline.Commands;
using OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors.Dns;
using OpenStatusPage.Server.Domain.Entities.Monitors.Http;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Shared.Enumerations;
using System.Collections.Concurrent;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks
{
    public partial class MonitorTask
    {
        private readonly string _monitorId;
        private readonly long _monitorVersion;
        private readonly ILogger _logger;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly CancellationTokenSource _cancellation;
        private readonly Task _runner;

        public MonitorTask(string monitorId, long monitorVersion, ILogger logger, ScopedMediatorExecutor scopedMediator)
        {
            _monitorId = monitorId;
            _monitorVersion = monitorVersion;
            _logger = logger;
            _scopedMediator = scopedMediator;
            _cancellation = new();

            _runner = Task.Run(DoWorkAsync);
        }

        public async Task CancelAsync()
        {
            _logger.LogDebug($"Task assignment for monitor({_monitorId}|Version {_monitorVersion}) was canceled.");

            _cancellation.Cancel();

            await _runner;
        }

        protected async Task DoWorkAsync()
        {
            var monitor = (await _scopedMediator.Send(new MonitorsQuery
            {
                Query = new(query => query
                    .Where(monitor => monitor.Id == _monitorId && monitor.Version >= _monitorVersion)
                    .Include(monitor => monitor.Rules))
            }, _cancellation.Token))?.Monitors?.FirstOrDefault();

            if (monitor == null)
            {
                _logger.LogError($"Unable to load monitor({_monitorId}|Version {_monitorVersion}) configuration. Aborting task ...");

                return;
            }

            if (monitor.Version > _monitorVersion)
            {
                _logger.LogDebug($"Task assignment for monitor({_monitorId}|Version {_monitorVersion}) was oudated on arrival. Monitor version found was {monitor.Version}. Ingoring task ...");

                return;
            }

            var checkType = monitor switch
            {
                DnsMonitor => typeof(DnsMonitorCheck),
                HttpMonitor => typeof(HttpMonitorCheck),
                PingMonitor => typeof(PingMonitorCheck),
                SshMonitor => typeof(SshMonitorCheck),
                TcpMonitor => typeof(TcpMonitorCheck),
                UdpMonitor => typeof(UdpMonitorCheck),
                _ => throw new NotImplementedException()
            };

            var checks = new ConcurrentQueue<Task<(DateTimeOffset, ServiceStatus)>>();
            var taskAddedEvent = new AsyncManualResetEvent(false);

            //Start non blocking dispatcher thread
            var dispatcher = Task.Run(async () =>
            {
                //Start at 00:00:00 of the current day and fast foward until we have reached a check now or in the future
                var performTime = DateTimeOffset.UtcNow.UtcDateTime.Date;
                while (performTime < DateTimeOffset.UtcNow) performTime += monitor.Interval;

                //While monitor task is running
                while (!_cancellation.IsCancellationRequested)
                {
                    //_logger.LogDebug($"Monitor({monitor.Name}|{monitor.Id}) was scheduled to be checked at {performTime}.");

                    //Wait until execution
                    var waitDuration = performTime - DateTimeOffset.UtcNow;
                    if (waitDuration > TimeSpan.Zero) await Task.Delay(waitDuration, _cancellation.Token);

                    //Create a new check instance that will handle the check
                    var check = Activator.CreateInstance(checkType) as MonitorCheckBase;

                    var currentStatus = await _scopedMediator.Send(new ServiceStatusQuery()
                    {
                        MonitorId = monitor.Id,
                        MonitorVersion = monitor.Version
                    });

                    //Perform check non blocking and queue for the consumer to await the task
                    checks.Enqueue(check.PerformAsync(monitor, performTime, currentStatus ?? ServiceStatus.Unknown, _logger, _cancellation.Token));

                    taskAddedEvent.Set(true);

                    performTime += monitor.Interval;
                }
            });

            //While monitor task is running
            while (!_cancellation.IsCancellationRequested)
            {
                await taskAddedEvent.WaitAsync(_cancellation.Token);

                while (checks.TryDequeue(out var checkTask))
                {
                    (var executionTime, var resultStatus) = await checkTask.WaitAsync(_cancellation.Token);

                    //Report the result
                    await _scopedMediator.Send(new AddLocalServiceStatusResultCmd()
                    {
                        MonitorId = monitor.Id,
                        MonitorVersion = monitor.Version,
                        DateTime = executionTime,
                        ServiceStatus = resultStatus
                    });
                }
            }

            //Wait for the dispatcher thread to join back
            await dispatcher;
        }
    }
}
