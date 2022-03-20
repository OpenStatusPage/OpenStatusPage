using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Cluster;

public class ClusterMembershipTests : TestBase
{
    public ClusterMembershipTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task JoinCluster_DirectJoin_JoinSuccessfulAsync()
    {
        // Arrange
        using var cluster = await SingleMemberCluster.CreateAsync(_testOutput);

        // Act
        using var follower1Host = ClusterBase.CreateFollower(cluster.LeaderHost, _testOutput);

        var follower1InitCompleted = new TaskCompletionSource();
        var clusterServicefollower1 = follower1Host.Services.GetRequiredService<ClusterService>();

        clusterServicefollower1.OnOperational += (sender, args) =>
        {
            follower1InitCompleted.TrySetResult();
        };

        await follower1Host.StartAsync();

        await follower1InitCompleted.Task.WaitAsync(_testWaitMax);

        // Assert
        Assert.Contains(await clusterServicefollower1.GetMembersAsync(), x =>
            x.IsLeader &&
            x.Endpoint.Equals(cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember().Endpoint));
    }

    [Fact]
    public async Task JoinCluster_ProxyJoin_JoinSuccessfulAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);

        // Act
        using var follower3Host = ClusterBase.CreateFollower(cluster.LeaderHost, _testOutput);

        var follower3InitCompleted = new TaskCompletionSource();
        var clusterServicefollower3 = follower3Host.Services.GetRequiredService<ClusterService>();

        clusterServicefollower3.OnOperational += (sender, args) =>
        {
            follower3InitCompleted.TrySetResult();
        };

        await follower3Host.StartAsync();

        await follower3InitCompleted.Task.WaitAsync(_testWaitMax);

        // Assert
        Assert.Contains(await clusterServicefollower3.GetMembersAsync(), x =>
            x.IsLeader &&
            x.Endpoint.Equals(cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember().Endpoint));
    }

    [Fact]
    public async Task LeaveCluster_Follower_LeftSuccessfulAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);

        // Act
        var local = cluster.Follower2Host.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower2Endpoint = local.Endpoint;

        var follower2Left = new TaskCompletionSource();
        var clusterServiceLeader = cluster.LeaderHost.Services.GetRequiredService<ClusterService>();

        clusterServiceLeader.OnMemberRemoved += (sender, args) =>
        {
            if (args.Member.Endpoint.Equals(follower2Endpoint))
            {
                follower2Left.TrySetResult();
            }
        };

        try
        {
            await cluster.Follower2Host.StopAsync();
        }
        catch
        {
        }

        await follower2Left.Task.WaitAsync(_testWaitMax);

        // Assert
        Assert.DoesNotContain(await clusterServiceLeader.GetMembersAsync(), x => x.Endpoint.Equals(follower2Endpoint));
        Assert.Equal(2, (await clusterServiceLeader.GetMembersAsync()).Count);
    }
}
