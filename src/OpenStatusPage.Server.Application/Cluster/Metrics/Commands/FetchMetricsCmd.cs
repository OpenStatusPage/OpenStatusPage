using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Cluster.Metrics.Commands
{
    public class FetchMetricsCmd : RequestBase<FetchMetricsCmd.Response>
    {
        public class Handler : IRequestHandler<FetchMetricsCmd, Response>
        {
            private readonly MetricsService _metricsService;

            public Handler(MetricsService metricsService)
            {
                _metricsService = metricsService;
            }

            public async Task<Response> Handle(FetchMetricsCmd request, CancellationToken cancellationToken)
            {
                return new()
                {
                    CpuAvg = await _metricsService.GetCpuAverageAsync()
                };
            }
        }

        public class Response
        {
            public double CpuAvg { get; set; }
        }
    }
}
