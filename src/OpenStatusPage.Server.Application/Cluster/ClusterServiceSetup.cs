using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenStatusPage.Server.Application.Cluster
{
    public static class ClusterServiceSetup
    {
        public static IServiceCollection AddClusterServices(this IServiceCollection services, IConfiguration configuration)
            => ClusterService.ConfigureServices(services, configuration);

        public static IHostBuilder UseClusterService(this IHostBuilder builder)
            => ClusterService.ConfigureHostBuilder(builder);

        public static IApplicationBuilder UseClusterService(this IApplicationBuilder builder)
            => ClusterService.ConfigureApplicationBuilder(builder);
    }
}
