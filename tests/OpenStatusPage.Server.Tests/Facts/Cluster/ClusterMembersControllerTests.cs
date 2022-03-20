using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.DataTransferObjects.Cluster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Cluster;

public class ClusterMembersControllerTests : TestBase
{
    public ClusterMembersControllerTests(ITestOutputHelper testOutput) : base(testOutput)
    {
    }

    [Fact]
    public async Task GetAll_ThreeMembers_AllMetadataExistsAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);
        var leaderMember = cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower1Member = cluster.Follower1Host.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower2Member = cluster.Follower2Host.Services.GetRequiredService<ClusterService>().GetLocalMember();

        // Act
        var metaDtos = await cluster.LeaderHttpClient.GetFromJsonAsync<List<ClusterMemberDto>>("api/v1/ClusterMembers");

        // Assert
        Assert.Equal(3, metaDtos.Count);
        Assert.Contains(metaDtos, x => x.Id.Equals(leaderMember.Id) && x.Endpoint.Equals(leaderMember.Endpoint) && x.Tags.ToHashSet().SetEquals(leaderMember.Tags) && x.IsLeader);
        Assert.Contains(metaDtos, x => x.Id.Equals(follower1Member.Id) && x.Endpoint.Equals(follower1Member.Endpoint) && x.Tags.ToHashSet().SetEquals(follower1Member.Tags));
        Assert.Contains(metaDtos, x => x.Id.Equals(follower2Member.Id) && x.Endpoint.Equals(follower2Member.Endpoint) && x.Tags.ToHashSet().SetEquals(follower2Member.Tags));
    }

    [Fact]
    public async Task GetTags_ThreeMembers_AllTagsReturnedAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);
        var leaderMember = cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower1Member = cluster.Follower1Host.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower2Member = cluster.Follower2Host.Services.GetRequiredService<ClusterService>().GetLocalMember();

        var tags = leaderMember.Tags
            .Concat(follower1Member.Tags)
            .Concat(follower2Member.Tags)
            .ToHashSet();

        // Act
        var apiTags = await cluster.LeaderHttpClient.GetFromJsonAsync<List<string>>("api/v1/ClusterMembers/tags");

        // Assert
        Assert.NotNull(apiTags);
        Assert.True(tags.SequenceEqual(apiTags!));
    }

    [Fact]
    public async Task DeleteMember_DeleteFollower2_Follower2RemovedAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);
        var leaderClusterService = cluster.LeaderHost.Services.GetRequiredService<ClusterService>();
        var follower2Member = cluster.Follower2Host.Services.GetRequiredService<ClusterService>().GetLocalMember();

        var follower2Removed = new TaskCompletionSource();
        leaderClusterService.OnMemberRemoved += (sender, args) =>
        {
            if (args.Member.Endpoint.Equals(follower2Member.Endpoint))
            {
                follower2Removed.TrySetResult();
            }
        };

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/ClusterMembers/{follower2Member.Id}")
        {
            Content = new StringContent(JsonSerializer.Serialize(follower2Member.Endpoint, follower2Member.Endpoint.GetType()), Encoding.UTF8, "application/json")
        };
        var response = await cluster.LeaderHttpClient.SendAsync(request);

        await follower2Removed.Task.WaitAsync(_testWaitMax);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccessStatusCode);
        Assert.Null(await leaderClusterService.GetMemberByIdAsync(follower2Member.Id));
    }


    [Fact]
    public async Task GetEndpoints_Unauthenticated_AllEndpointsReturnedAsync()
    {
        // Arrange
        using var cluster = await RedundantCluster.CreateAsync(_testOutput);
        var leaderMember = cluster.LeaderHost.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower1Member = cluster.Follower1Host.Services.GetRequiredService<ClusterService>().GetLocalMember();
        var follower2Member = cluster.Follower2Host.Services.GetRequiredService<ClusterService>().GetLocalMember();

        var unauthenticatedHttpClient = new HttpClient { BaseAddress = cluster.LeaderHttpClient.BaseAddress };

        // Act
        var endpoints = await unauthenticatedHttpClient.GetFromJsonAsync<List<Uri>>("api/v1/ClusterMembers/public/endpoints");

        // Assert
        Assert.Equal(3, endpoints.Count);
        Assert.Contains(endpoints, x => x.Equals(leaderMember.Endpoint));
        Assert.Contains(endpoints, x => x.Equals(follower1Member.Endpoint));
        Assert.Contains(endpoints, x => x.Equals(follower2Member.Endpoint));
    }
}
