using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Http
{
    public class SslCertificateRuleDto : MonitorRuleDto
    {
        public SslCertificateCheckType CheckType { get; set; }

        public TimeSpan? MinValidTimespan { get; set; }
    }
}
