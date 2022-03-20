using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Authentication;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Helpers;

public class SingleMemberCluster : ClusterBase, IDisposable
{
    public IHost LeaderHost { get; private set; }

    public HttpClient LeaderHttpClient { get; private set; }

    public static async Task<SingleMemberCluster> CreateAsync(ITestOutputHelper testOutput)
    {
        var cluster = new SingleMemberCluster();
        await cluster.InitializeAsync(testOutput);
        return cluster;
    }

    public virtual async Task InitializeAsync(ITestOutputHelper testOutput)
    {
        testOutput.WriteLine($"SingleMemberCluster::InitializeAsync() Starting leader ...");

        LeaderHost = CreateLeader(testOutput);

        var host1InitCompleted = new TaskCompletionSource();
        var clusterServiceHost1 = LeaderHost.Services.GetRequiredService<ClusterService>();

        clusterServiceHost1.OnOperational += (sender, args) =>
        {
            host1InitCompleted.TrySetResult();
        };

        await LeaderHost.StartAsync();

        await host1InitCompleted.Task.WaitAsync(TestBase._testWaitMax);

        var leaderSettings = LeaderHost.Services.GetRequiredService<EnvironmentSettings>();

        LeaderHttpClient = new HttpClient { BaseAddress = leaderSettings.PublicEndpoint };
        LeaderHttpClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HEADER_NAME, leaderSettings.ApiKey);

        testOutput.WriteLine($"SingleMemberCluster::InitializeAsync() Finished.");
    }

    public virtual void Dispose()
    {
        try
        {
            LeaderHttpClient?.CancelPendingRequests();
            LeaderHost?.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }
    }
}
