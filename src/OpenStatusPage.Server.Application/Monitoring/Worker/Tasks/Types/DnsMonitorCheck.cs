using DnsClient;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Dns;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Utilities;
using System.Net;

namespace OpenStatusPage.Server.Application.Monitoring.Worker.Tasks.Types
{
    public class DnsMonitorCheck : MonitorCheckBase
    {
        protected override async Task<ServiceStatus> DoCheckAsync(MonitorBase monitor, CancellationToken cancellationToken)
        {
            if (monitor is not DnsMonitor dnsMonitor) throw new Exception($"Invalid monitor type assigned to {nameof(DnsMonitorCheck)}");

            //Get the custom resolvers
            var customResolvers = dnsMonitor.Resolvers?
                .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => IPAddress.TryParse(x, out var _))
                .Select(x => IPAddress.Parse(x))
                .ToArray();

            //Create dns client with custom nameservers if we have any specified
            var dnsClient = (customResolvers != null && customResolvers.Length > 0) ? new LookupClient(customResolvers) : new();

            //Translate own types to library
            var recordType = dnsMonitor.RecordType switch
            {
                DnsRecordType.A => QueryType.A,
                DnsRecordType.AAAA => QueryType.AAAA,
                DnsRecordType.CNAME => QueryType.CNAME,
                DnsRecordType.MX => QueryType.MX,
                DnsRecordType.NS => QueryType.NS,
                DnsRecordType.PTR => QueryType.PTR,
                DnsRecordType.SRV => QueryType.SRV,
                DnsRecordType.TXT => QueryType.TXT,
                _ => throw new NotImplementedException()
            };

            //Prepare dns query
            var query = new DnsQuestion(dnsMonitor.Hostname, recordType, QueryClass.IN);

            //Set timeout if defined
            var options = dnsMonitor.Timeout.HasValue ? new DnsQueryAndServerOptions { Timeout = dnsMonitor.Timeout.Value } : new();

            //Send dns query
            var records = (await dnsClient.QueryAsync(query, options, cancellationToken))?.Answers.ToList();

            //No records found
            if (records == null || records.Count == 0) return ServiceStatus.Unavailable;

            //Evaluate all the rules
            foreach (var rule in dnsMonitor.Rules.OrderBy(x => x.OrderIndex))
            {
                switch (rule)
                {
                    case DnsRecordRule dnsRecordRule:
                    {
                        if (records.Any(x => StringCompareHelper.Compare(x.ToString(), dnsRecordRule.ComparisonType, dnsRecordRule.ComparisonValue)))
                        {
                            return dnsRecordRule.ViolationStatus;
                        }

                        break;
                    }

                    default: throw new NotImplementedException();
                }
            }

            //Default result
            return ServiceStatus.Available;
        }
    }
}
