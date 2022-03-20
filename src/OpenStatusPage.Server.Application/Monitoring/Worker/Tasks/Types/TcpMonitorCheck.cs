using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Tcp;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Diagnostics;
using System.Net.Sockets;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class TcpMonitorCheck : MonitorCheckBase
    {
        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not TcpMonitor tcpMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(TcpMonitorCheck)}");

            try
            {
                using var client = new TcpClient();

                //Prepare timeout token
                if (tcpMonitor.Timeout.HasValue)
                {
                    cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(tcpMonitor.Timeout.Value).Token).Token;
                }

                //Start timer to measure connect time
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var connectTask = client.ConnectAsync(tcpMonitor.Hostname, tcpMonitor.Port, cancellationToken);

                await connectTask.AsTask().WaitAsync(cancellationToken);

                //Get the response time
                var responseTime = stopwatch.Elapsed.TotalMilliseconds;
                stopwatch.Stop();

                client.Close();

                //TCP connection was not successful during timeout
                if (!connectTask.IsCompleted) return ServiceStatus.Unavailable;

                //Evaluate all the rules
                foreach (var rule in tcpMonitor.Rules.OrderBy(x => x.OrderIndex))
                {
                    switch (rule)
                    {
                        case ResponseTimeRule responseTimeRule:
                        {
                            if (NumberCompareHelper.Compare(responseTime, responseTimeRule.ComparisonType, responseTimeRule.ComparisonValue))
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
