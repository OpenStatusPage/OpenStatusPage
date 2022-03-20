using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Cluster.Consensus;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States;
using OpenStatusPage.Server.Application.Incidents.Commands;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.Notifications.History.Commands;
using OpenStatusPage.Server.Application.Notifications.Providers.Commands;
using OpenStatusPage.Server.Application.StatusHistory.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ssh;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Facts.Incidents;
using OpenStatusPage.Server.Tests.Facts.Notifications;
using OpenStatusPage.Server.Tests.Helpers;
using OpenStatusPage.Shared.Enumerations;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Cluster;

public class SnapshotTests : TestBase, IDisposable
{
    protected readonly SingleMemberCluster _cluster;
    protected readonly ClusterService _leaderClusterService;
    protected readonly ScopedMediatorExecutor _leaderMediator;

    public SnapshotTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = SingleMemberCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _leaderClusterService = _cluster.LeaderHost.Services.GetRequiredService<ClusterService>();
        _leaderMediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    [Fact]
    public async Task TestSnapshots_DataForEachDbSet_TaskAssignedResultReportedNotificationSentAsync()
    {
        // Arrange
        var monitorid = Guid.NewGuid().ToString();
        await _leaderClusterService.ReplicateAsync(new CreateOrUpdateMonitorCmd
        {
            Data = new SshMonitor()
            {
                Id = monitorid,
                Enabled = true,
                Name = "SshMonitor",
                Tags = "",
                Hostname = "127.0.0.1",
                Username = "root",
                Password = "password",
                PrivateKey = "OPENSSHKEY",
                Command = "ls",
                Interval = TimeSpan.FromDays(1),
                WorkerCount = 1,
                Rules = Array.Empty<MonitorRule>(),
                NotificationProviders = Array.Empty<NotificationProvider>()
            }
        });

        await _leaderClusterService.ReplicateAsync(new CreateOrUpdateIncidentCmd
        {
            Data = IncidentsControllerTests.CreateIncident()
        });

        await _leaderClusterService.ReplicateAsync(new CreateOrUpdateNotificationProviderCmd
        {
            Data = NotificationProvidersControllerTests.CreateWebhookProvider()
        });

        await _leaderClusterService.ReplicateAsync(new CreateStatusHistoryRecordCmd
        {
            MonitorId = monitorid,
            Status = ServiceStatus.Available,
            UtcFrom = DateTimeOffset.UtcNow.AddDays(-7).UtcDateTime
        });

        await _leaderClusterService.ReplicateAsync(new CreateNotificationHistoryRecordCmd
        {
            MonitorId = monitorid,
            StatusUtc = DateTimeOffset.UtcNow.AddDays(-7).UtcDateTime
        });

        // Act
        var raftLog = _cluster.LeaderHost.Services.GetRequiredService<PersistentMessageReplicatorState>();
        await raftLog.ForceCompactionAsync();

        using var follower1Host = ClusterBase.CreateFollower(_cluster.LeaderHost, _testOutput);
        var followerMediator = follower1Host.Services.GetRequiredService<ScopedMediatorExecutor>();

        var follower1SnapshotProcessed = new TaskCompletionSource();
        var clusterServicefollower1 = follower1Host.Services.GetRequiredService<ClusterService>();

        clusterServicefollower1.OnReplicatedMessage += (sender, args) =>
        {
            if (args is DataSnapshotCmd dataSnapshot)
            {
                follower1SnapshotProcessed.TrySetResult();
            }
        };

        await follower1Host.StartAsync();

        await follower1SnapshotProcessed.Task.WaitAsync(_testWaitMax);

        // Assert 
        Assert.Equal((await _leaderMediator.Send(new MonitorsQuery())).Monitors.Count, (await followerMediator.Send(new MonitorsQuery())).Monitors.Count);
        Assert.Equal((await _leaderMediator.Send(new IncidentsQuery())).Incidents.Count, (await followerMediator.Send(new IncidentsQuery())).Incidents.Count);
        Assert.Equal((await _leaderMediator.Send(new NotificationProvidersQuery())).NotificationProviders.Count, (await followerMediator.Send(new NotificationProvidersQuery())).NotificationProviders.Count);
        Assert.Equal((await _leaderMediator.Send(new StatusHistoriesQuery())).HistoryRecords.Count, (await followerMediator.Send(new StatusHistoriesQuery())).HistoryRecords.Count);
        Assert.Equal((await _leaderMediator.Send(new NotificationHistoriesQuery())).NotificationHistoryRecords.Count, (await followerMediator.Send(new NotificationHistoriesQuery())).NotificationHistoryRecords.Count);
    }
}
