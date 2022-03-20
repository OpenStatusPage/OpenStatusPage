using OpenStatusPage.Shared.Enumerations;
using System;

namespace OpenStatusPage.Server.Domain.Entities.Monitors.Http
{
    public class SslCertificateRule : MonitorRule
    {
        public SslCertificateCheckType CheckType { get; set; }

        public TimeSpan? MinValidTimespan { get; set; }
    }
}
