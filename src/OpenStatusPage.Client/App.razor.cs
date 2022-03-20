using Microsoft.AspNetCore.Components;
using OpenStatusPage.Client.Application;

namespace OpenStatusPage.Client
{
    public partial class App
    {
        //Services automatically started as part of the app

        [Inject]
        public ClusterEndpointsService ClusterStatusService { get; set; }
    }
}