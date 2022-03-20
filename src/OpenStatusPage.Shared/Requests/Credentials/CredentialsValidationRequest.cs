using OpenStatusPage.Shared.Models.Credentials;

namespace OpenStatusPage.Shared.Requests.Credentials
{
    public class CredentialsValidationRequest
    {
        public DashboardCredentials? DashboardCredentials { get; set; }

        public List<StatusPageCredentials>? StatusPageCredentials { get; set; }

        public class Response
        {
            public DashboardCredentials ValidDashboardCredentials { get; set; }

            public List<StatusPageCredentials> ValidStatusPageCredentials { get; set; }
        }
    }
}
