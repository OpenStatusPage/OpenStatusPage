using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Net.NetworkInformation;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class PingMonitorCheck : MonitorCheckBase
    {
        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not PingMonitor pingMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(PingMonitorCheck)}");

            //Prepare timeout token
            try
            {
                var result = pingMonitor.Timeout.HasValue ?
                await new Ping().SendPingAsync(pingMonitor.Hostname, (int)pingMonitor.Timeout!.Value.TotalMilliseconds) :
                await new Ping().SendPingAsync(pingMonitor.Hostname);

                //If ping failed mark the service as unavailable
                if (result == null || result.Status != IPStatus.Success) return ServiceStatus.Unavailable;

                //Evaluate all the rules
                foreach (var rule in pingMonitor.Rules.OrderBy(x => x.OrderIndex))
                {
                    switch (rule)
                    {
                        case ResponseTimeRule responseTimeRule:
                        {
                            if (NumberCompareHelper.Compare(result.RoundtripTime, responseTimeRule.ComparisonType, responseTimeRule.ComparisonValue))
                            {
                                return responseTimeRule.ViolationStatus;
                            }

                            break;
                        }

                        default: throw new NotImplementedException();
                    }
                }
            }
            catch
            {
                return ServiceStatus.Unavailable;
            }

            //Default result
            return ServiceStatus.Available;
        }
    }
}
