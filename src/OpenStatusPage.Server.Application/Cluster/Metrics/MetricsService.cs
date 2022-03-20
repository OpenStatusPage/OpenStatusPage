using Microsoft.Extensions.Hosting;
using OpenStatusPage.Shared.Interfaces;
using System.Diagnostics;

namespace OpenStatusPage.Server.Application.Cluster.Metrics
{
    public class MetricsService : ISingletonService, IHostedService, IDisposable
    {
        protected static readonly TimeSpan _interval = TimeSpan.FromSeconds(10);
        protected static readonly TimeSpan _maxHistory = TimeSpan.FromMinutes(1);

        private Timer _timer = null!;

        protected Queue<double> CpuPercentages { get; set; } = new();

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer((state) => _ = Task.Run(CollectMetricsAsync), null, TimeSpan.Zero, _interval);

            return Task.CompletedTask;
        }

        protected async Task CollectMetricsAsync()
        {
            CpuPercentages.Enqueue(await CalculateCpuUsageAsync());

            //Keep only the most recent 
            while (CpuPercentages.Count * _interval > _maxHistory)
            {
                CpuPercentages.Dequeue();
            }
        }

        /// <summary>
        /// Returns the avg cpu usage as range from 0.00 to 100.00%
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetCpuAverageAsync()
        {
            //Make an explicit copy to work on to avoid concurrency issues and stay non blocking
            var data = CpuPercentages.ToList();

            return Math.Clamp(Math.Round(data.Count > 0 ? data.Sum(x => x) / data.Count : 0, 2), 0.00, 100.00);
        }

        protected static async Task<double> CalculateCpuUsageAsync()
        {
            //Get start timestamp and the total processing time of all known processes
            var start = DateTimeOffset.UtcNow;
            var before = Process.GetCurrentProcess().TotalProcessorTime;

            //Waith half of the interval to measure
            await Task.Delay(_interval / 2);

            //Get end timestamp and how much processing time was used by now
            var end = DateTimeOffset.UtcNow;
            var after = Process.GetCurrentProcess().TotalProcessorTime;

            //Get total amount of milliseconds the cpu was used by all processes during the 1 second delay.
            var cpuTime = (after - before).Milliseconds;

            //Get the amount of time that elapsed. We can't hardcode this, as the task delay is not perfectly accurate
            var elapsed = (end - start).TotalMilliseconds;

            //(Total cpu time used / (measuring time * available parallel processors)) as percentage
            return cpuTime / (Environment.ProcessorCount * elapsed) * 100;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
