using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using Renci.SshNet;
using System.Text;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class SshMonitorCheck : MonitorCheckBase
    {
        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not SshMonitor sshMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(SshMonitorCheck)}");

            //Init auth methods with username only
            var authMethods = new List<AuthenticationMethod>()
            {
                new NoneAuthenticationMethod(sshMonitor.Username)
            };

            //Add password authentication if provided
            if (!string.IsNullOrWhiteSpace(sshMonitor.Password))
            {
                authMethods.Add(new PasswordAuthenticationMethod(sshMonitor.Username, sshMonitor.Password));
            }

            //Add private key authentication if provided
            if (!string.IsNullOrWhiteSpace(sshMonitor.PrivateKey))
            {
                authMethods.Add(new PrivateKeyAuthenticationMethod(sshMonitor.Username, new PrivateKeyFile[] { new(new MemoryStream(Encoding.UTF8.GetBytes(sshMonitor.PrivateKey))) }));
            }

            //Prepare connection info with optional port
            var connectionInfo = sshMonitor.Port.HasValue ?
                new ConnectionInfo(sshMonitor.Hostname, (int)sshMonitor.Port!, sshMonitor.Username, authMethods.ToArray()) :
                new ConnectionInfo(sshMonitor.Hostname, sshMonitor.Username, authMethods.ToArray());

            //Set timeout if specified
            if (sshMonitor.Timeout.HasValue) connectionInfo.Timeout = sshMonitor.Timeout.Value;

            try
            {
                using var client = new SshClient(connectionInfo);

                client.Connect();

                //If we could not connect, return unavailable. Might be because host is not reachable, or because login credentials were invalid
                if (!client.IsConnected) return ServiceStatus.Unavailable;

                var commandResult = "";
                if (!string.IsNullOrWhiteSpace(sshMonitor.Command))
                {
                    var command = client.CreateCommand(sshMonitor.Command);

                    if (sshMonitor.Timeout.HasValue) command.CommandTimeout = sshMonitor.Timeout.Value;

                    commandResult = await Task.FromResult(command.Execute());
                }

                client.Disconnect();
                client.Dispose();

                //Evaluate all the rules
                foreach (var rule in sshMonitor.Rules.OrderBy(x => x.OrderIndex))
                {
                    switch (rule)
                    {
                        case SshCommandResultRule sshCommandResultRule:
                        {
                            if (StringCompareHelper.Compare(commandResult, sshCommandResultRule.ComparisonType, sshCommandResultRule.ComparisonValue))
                            {
                                return sshCommandResultRule.ViolationStatus;
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
