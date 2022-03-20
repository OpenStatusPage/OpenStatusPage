using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Udp;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Diagnostics;
using System.Net.Sockets;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class UdpMonitorCheck : MonitorCheckBase
    {
        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not UdpMonitor udpMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(UdpMonitorCheck)}");

            try
            {
                using var udpClient = new UdpClient();

                //Supress ICMP messages to avoid exception (local network only)
                udpClient.Client.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);

                //Connect
                udpClient.Connect(udpMonitor.Hostname, udpMonitor.Port);

                //Start timer to measure connect time
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                //Send request
                await udpClient.SendAsync(udpMonitor.RequestBytes, udpMonitor.RequestBytes.Length);

                var timeoutCtx = new CancellationTokenSource(udpMonitor.Timeout ?? TimeSpan.FromSeconds(120));

                //Try reading incoming messages within the timeout. There could be spam coming from other servers, so we can't be sure the respose we need will be the first one
                while (!timeoutCtx.IsCancellationRequested)
                {
                    var result = await udpClient.ReceiveAsync(timeoutCtx.Token);

                    //Only accept udp messages from the original sender
                    if (result.RemoteEndPoint.Equals(udpClient.Client.RemoteEndPoint))
                    {
                        //Get the response time
                        var responseTime = stopwatch.Elapsed.TotalMilliseconds;
                        stopwatch.Stop();

                        //Evaluate all the rules
                        foreach (var rule in udpMonitor.Rules.OrderBy(x => x.OrderIndex))
                        {
                            switch (rule)
                            {
                                case ResponseBytesRule responseBytesRule:
                                {
                                    if (!result.Buffer.SequenceEqual(responseBytesRule.ExpectedBytes))
                                    {
                                        return ServiceStatus.Unavailable;
                                    }

                                    break;
                                }

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

                        return ServiceStatus.Available;
                    }
                }

                udpClient.Close();
            }
            catch
            {
            }

            //Default result
            return ServiceStatus.Unavailable;
        }
    }
}
