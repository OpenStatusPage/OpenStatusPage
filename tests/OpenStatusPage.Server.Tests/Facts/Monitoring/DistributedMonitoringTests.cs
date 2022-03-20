using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Application.Monitoring.Coordination.Commands;
using OpenStatusPage.Server.Application.Monitors.Commands;
using OpenStatusPage.Server.Application.Notifications.History.Commands;
using OpenStatusPage.Server.Domain.Entities.Monitors;
using OpenStatusPage.Server.Domain.Entities.Monitors.Ping;
using OpenStatusPage.Server.Domain.Entities.Notifications.Providers;
using OpenStatusPage.Server.Tests.Helpers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OpenStatusPage.Server.Tests.Facts.Monitoring;

public class DistributedMonitoringTests : TestBase, IDisposable
{
    protected readonly RedundantCluster _cluster;
    protected readonly ClusterService _leaderClusterService;
    protected readonly ScopedMediatorExecutor _leaderMediator;

    public DistributedMonitoringTests(ITestOutputHelper testOutput) : base(testOutput)
    {
        _cluster = RedundantCluster.CreateAsync(testOutput).GetAwaiter().GetResult();
        _leaderClusterService = _cluster.LeaderHost.Services.GetRequiredService<ClusterService>();
        _leaderMediator = _cluster.LeaderHost.Services.GetRequiredService<ScopedMediatorExecutor>();
    }

    public void Dispose()
    {
        _cluster.Dispose();
    }

    [Fact]
    public async Task TestDistributedMonitor_EnabledMonitorMatchingTags_TaskAssignedResultReportedNotificationSentAsync()
    {
        // Arrange
        var monitor = new PingMonitor()
        {
            Id = Guid.NewGuid().ToString(),
            Enabled = true,
            Name = "LocalPing",
            Tags = "local",
            Hostname = "127.0.0.1",
            Interval = TimeSpan.FromSeconds(5),
            WorkerCount = 3,
            Rules = Array.Empty<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };

        var monitoringChainCompleted = new TaskCompletionSource();

        _leaderClusterService.OnReplicatedMessage += (sender, args) =>
        {
            if (args is CreateNotificationHistoryRecordCmd notificationHistoryRecordCmd && notificationHistoryRecordCmd.MonitorId == monitor.Id)
            {
                monitoringChainCompleted.TrySetResult();
            }
        };

        // Act
        await _leaderClusterService.ReplicateAsync(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        // Assert 
        await monitoringChainCompleted.Task.WaitAsync(_testWaitMax);
    }

    [Fact]
    public async Task TestMonitorDelete_EnabledMonitor_TaskAssignedCancelledAsync()
    {
        // Arrange
        var monitor = new PingMonitor()
        {
            Id = Guid.NewGuid().ToString(),
            Enabled = true,
            Name = "LocalPing",
            Tags = "local",
            Hostname = "127.0.0.1",
            Interval = TimeSpan.FromDays(1),
            WorkerCount = 3,
            Rules = Array.Empty<MonitorRule>(),
            NotificationProviders = Array.Empty<NotificationProvider>()
        };

        var tasksAssigned = new TaskCompletionSource();
        _leaderClusterService.OnReplicatedMessage += (sender, args) =>
        {
            if (args is TaskAssignmentCmd taskAssignment && taskAssignment.MonitorId == monitor.Id)
            {
                tasksAssigned.TrySetResult();
            }
        };

        var monitorDeleted = new TaskCompletionSource();
        _leaderClusterService.OnReplicatedMessage += (sender, args) =>
        {
            if (args is DeleteMonitorCmd deleteMonitorCmd && deleteMonitorCmd.MonitorId == monitor.Id)
            {
                monitorDeleted.TrySetResult();
            }
        };

        await _leaderClusterService.ReplicateAsync(new CreateOrUpdateMonitorCmd
        {
            Data = monitor
        });

        await tasksAssigned.Task.WaitAsync(_testWaitMax);

        // Act
        await _leaderClusterService.ReplicateAsync(new DeleteMonitorCmd
        {
            MonitorId = monitor.Id,
        });

        await monitorDeleted.Task.WaitAsync(_testWaitMax);

        // Assert 
        Assert.Empty((await _leaderMediator.Send(new TaskAssignmentsQuery())).TaskAssignments);
    }

}
