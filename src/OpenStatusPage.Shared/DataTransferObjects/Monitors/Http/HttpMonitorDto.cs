using OpenStatusPage.Shared.Enumerations;

namespace OpenStatusPage.Shared.DataTransferObjects.Monitors.Http
{
    public class HttpMonitorDto : MonitorDto
    {
        public string Url { get; set; }

        public string Method { get; set; }

        public ushort MaxRedirects { get; set; }

        public string? Headers { get; set; }

        public string? Body { get; set; }

        public HttpAuthenticationScheme AuthenticationScheme { get; set; }

        public string? AuthenticationBase { get; set; }

        public string? AuthenticationAdditional { get; set; }

        public List<ResponseTimeRuleDto> ResponseTimeRules { get; set; }

        public List<ResponseBodyRuleDto> ResponseBodyRules { get; set; }

        public List<ResponseHeaderRuleDto> ResponseHeaderRules { get; set; }

        public List<SslCertificateRuleDto> SslCertificateRules { get; set; }

        public List<StatusCodeRuleDto> StatusCodeRules { get; set; }
    }
}
