using OpenStatusPage.Shared.DataTransferObjects.Incidents;

namespace OpenStatusPage.Shared.Requests.Incidents
{
    public class IncidentsForServicesRequest
    {
        public List<string> ServiceIds { get; set; }

        public DateTimeOffset From { get; set; }

        public DateTimeOffset Until { get; set; }

        public class Response
        {
            public List<IncidentDto> Incidents { get; set; }
        }
    }
}
