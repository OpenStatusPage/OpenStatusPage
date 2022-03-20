using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks
{
    public class MonitorCheckBase
    {
        protected virtual async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken) => ServiceStatus.Unknown;

        public async Task<(DateTimeOffset, ServiceStatus)> PerformAsync(MonitorBase monitor, DateTimeOffset scheduledExecution, ServiceStatus lastKnownStatus, ILogger logger, CancellationToken cancellationToken)
        {
            var lastCheckExecuted = scheduledExecution;
            var currentStatus = ServiceStatus.Unknown;

            var iterations = 1;

            //Add retires, unless the monitor is already in a state that does not need to be confirmed again
            if (lastKnownStatus != ServiceStatus.Unknown && lastKnownStatus != ServiceStatus.Unavailable)
            {
                iterations += monitor.Retries ?? 0;
            }

            for (int nCheck = 0; nCheck < iterations && !cancellationToken.IsCancellationRequested; nCheck++)
            {
                if (nCheck > 0)
                {
                    logger.LogDebug(
                        $"LOCAL Worker status for monitor({monitor.Name}|{monitor.Id}|Version {monitor.Version}) is '{Enum.GetName(currentStatus)}' at {lastCheckExecuted.DateTime}." +
                        $" It will be confirmed {iterations - nCheck} more time{(iterations - nCheck > 1 ? "s" : "")} before it is reported.");

                    lastCheckExecuted += monitor.RetryInterval!.Value;

                    //The first check will be done immediately. Any additional retires, will have to wait until their execution time
                    var waitDuration = lastCheckExecuted - DateTimeOffset.UtcNow;
                    if (waitDuration > TimeSpan.Zero) await Task.Delay(waitDuration, cancellationToken);
                }

                currentStatus = await DoCheckAsync(monitor, cancellationToken).WaitAsync(cancellationToken);

                //If we have a status that does not need to be confirmed, we can break, regardless of retries remaining
                if (currentStatus == ServiceStatus.Available || currentStatus == ServiceStatus.Degraded) break;
            }

            //return (lastCheckExecuted, currentStatus);

            //Report for the original scheduled time, even if retires were made
            return (scheduledExecution, currentStatus);
        }
    }
}
