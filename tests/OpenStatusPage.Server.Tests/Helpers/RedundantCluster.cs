using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenStatusPage.Server.Application.Authentication;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Helpers;

public class RedundantCluster : SingleMemberCluster
{
    public IHost Follower1Host { get; private set; }

    public IHost Follower2Host { get; private set; }

    public HttpClient Follower1HttpClient { get; private set; }

    public HttpClient Follower2HttpClient { get; private set; }

    public static new async Task<RedundantCluster> CreateAsync(ITestOutputHelper testOutput)
    {
        var cluster = new RedundantCluster();
        await cluster.InitializeAsync(testOutput);
        return cluster;
    }

    public override async Task InitializeAsync(ITestOutputHelper testOutput)
    {
        await base.InitializeAsync(testOutput);

        //Wait for the two members to be known by the leader
        var clusterServiceLeader = LeaderHost.Services.GetRequiredService<ClusterService>();
        var followersKnown = new TaskCompletionSource();

        clusterServiceLeader.OnMemberAdded += (sender, args) =>
        {
            if (sender is ClusterService clusterService)
            {
                if (clusterService.GetMembersAsync().GetAwaiter().GetResult().Count == 3) followersKnown.TrySetResult();
            }
        };

        //Follower 1 
        testOutput.WriteLine($"RedundantCluster::InitializeAsync() Starting Follower 1 ...");

        Follower1Host = CreateFollower(LeaderHost, testOutput);

        var follower1InitCompleted = new TaskCompletionSource();
        var clusterServicefollower1 = Follower1Host.Services.GetRequiredService<ClusterService>();

        clusterServicefollower1.OnOperational += (sender, args) =>
        {
            follower1InitCompleted.TrySetResult();
        };

        await Follower1Host.StartAsync();

        await follower1InitCompleted.Task.WaitAsync(TestBase._testWaitMax);

        var follower1Settings = Follower1Host.Services.GetRequiredService<EnvironmentSettings>();

        Follower1HttpClient = new HttpClient { BaseAddress = follower1Settings.PublicEndpoint };
        Follower1HttpClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HEADER_NAME, follower1Settings.ApiKey);

        //Follower 2
        testOutput.WriteLine($"RedundantCluster::InitializeAsync() Starting Follower 2 ...");

        Follower2Host = CreateFollower(LeaderHost, testOutput);

        var follower2InitCompleted = new TaskCompletionSource();
        var clusterServicefollower2 = Follower2Host.Services.GetRequiredService<ClusterService>();

        clusterServicefollower2.OnOperational += (sender, args) =>
        {
            follower2InitCompleted.TrySetResult();
        };

        await Follower2Host.StartAsync();

        await follower2InitCompleted.Task.WaitAsync(TestBase._testWaitMax);

        var follower2Settings = Follower2Host.Services.GetRequiredService<EnvironmentSettings>();

        Follower2HttpClient = new HttpClient { BaseAddress = follower2Settings.PublicEndpoint };
        Follower2HttpClient.DefaultRequestHeaders.Add(ApiKeyAuthenticationOptions.HEADER_NAME, follower2Settings.ApiKey);

        await followersKnown.Task.WaitAsync(TestBase._testWaitMax);

        testOutput.WriteLine($"RedundantCluster::InitializeAsync() Finished.");
    }

    public override void Dispose()
    {
        try
        {
            Follower1HttpClient?.CancelPendingRequests();
            Follower1Host?.StopAsync().GetAwaiter().GetResult();

            Follower2HttpClient?.CancelPendingRequests();
            Follower2Host?.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
        }

        base.Dispose();
    }
}
