using OpenStatusPage.Shared.DataTransferObjects.Services;

namespace OpenStatusPage.Shared.Requests.Services
{
    public class ServiceStatusHistoryRequest
    {
        public List<string> ServiceIds { get; set; }

        public DateTimeOffset From { get; set; }

        public DateTimeOffset Until { get; set; }

        public class Response
        {
            public List<ServiceStatusHistorySegmentDto> ServiceStatusHistories { get; set; }
        }
    }
}
