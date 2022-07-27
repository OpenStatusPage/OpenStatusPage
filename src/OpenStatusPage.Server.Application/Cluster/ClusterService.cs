using DotNext.Net.Cluster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft;
using OpenStatusPage.Server.Application.Cluster.Discovery.Commands;
using OpenStatusPage.Server.Application.Cluster.Discovery.Events;
using OpenStatusPage.Server.Application.Cluster.Metrics.Commands;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.Misc.Exceptions;
using OpenStatusPage.Server.Application.Misc.Mediator;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Shared.Enumerations;
using OpenStatusPage.Shared.Interfaces;
using OpenStatusPage.Shared.Utilities;
using System.Collections.ObjectModel;

namespace OpenStatusPage.Server.Application.Cluster
{
    public class ClusterService : IHostedService, ISingletonService
    {
        private readonly ILogger<ClusterService> _logger;
        private readonly INetworkConnector _networkConnector;
        private readonly RaftService _raftService;
        private readonly ScopedMediatorExecutor _scopedMediator;
        private readonly EnvironmentSettings _environmentSettings;

        protected List<ClusterMember> Members { get; } = new();

        public event EventHandler<ClusterMemberJoinedEventArgs> OnMemberAdded;

        public event EventHandler<ClusterMemberLeftEventArgs> OnMemberRemoved;

        public event EventHandler<Discovery.Events.ClusterMemberStatusChangedEventArgs> OnMemberStatusChanged;

        public event EventHandler<ClusterLeaderChangedEventArgs> OnClusterLeaderChanged;

        public event EventHandler<MessageBase> OnReplicatedMessage;

        /// <summary>
        /// Event for when the cluster service is initialized. 
        /// Handle cluster before it becomes operational
        /// </summary>
        public event EventHandler OnInitialized;

        /// <summary>
        /// Event for when the cluster service is operational. Can be triggered multiple times. 
        /// Members and data are in sync and the local member can start serving cluster based operations.
        /// </summary>
        public event EventHandler OnOperational;

        public bool IsOperational { get; set; }

        protected bool InitializedDispatched { get; set; }

        protected CancellationTokenSource ShutDownSource { get; set; } = new();

        protected TaskCompletionSource JoinLoopResult { get; set; }

        protected Debouncer JoinClusterDebouncer { get; set; } = new();

        protected bool IsRunning { get; set; }

        protected SemaphoreSlim MembershipLock { get; set; } = new(1);

        public ClusterService(
            ILogger<ClusterService> logger,
            INetworkConnector networkConnector,
            RaftService raftService,
            ScopedMediatorExecutor scopedMediator,
            EnvironmentSettings environmentSettings)
        {
            _logger = logger;
            _networkConnector = networkConnector;
            _raftService = raftService;
            _scopedMediator = scopedMediator;
            _environmentSettings = environmentSettings;
            _raftService.OnStop += async (sender, args) => await RaftOnStopAsync();
            _raftService.OnMemberJoined += async (sender, clusterMember) => await RaftOnMemberJoinedAsync(sender, clusterMember);
            _raftService.OnMemberLeft += async (sender, clusterMember) => await RaftOnMemberLeftAsync(sender, clusterMember);
            _raftService.OnLeaderChanged += async (sender, clusterMember) => await RaftOnLeaderChangedAsync(sender, clusterMember);
            _raftService.OnReplicatedMessage += (sender, message) => RaftOnReplicatedMessage(sender, message);
            _raftService.OnMessageQueueCleared += (sender, args) => RaftOnMessageQueueCleared();
        }

        public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
            => RaftService.ConfigureServices(services, configuration);

        public static IHostBuilder ConfigureHostBuilder(IHostBuilder builder)
            => RaftService.ConfigureHostBuilder(builder);

        public static IApplicationBuilder ConfigureApplicationBuilder(IApplicationBuilder app)
            => RaftService
                .ConfigureApplicationBuilder(app)
                .UseMiddleware<ClusterOperationalMiddleware>()
                .Use(next => new LeaderRedirection(next).Redirect);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IsRunning = true;

            _logger.LogInformation($"Starting cluster service with local Member(ID:{_environmentSettings.Id} | {_environmentSettings.PublicEndpoint}) and group tags [{(_environmentSettings.Tags.Count > 0 ? string.Join("|", _environmentSettings.Tags) : "none")}] ...");
        }

        protected async Task JoinLoopAsync()
        {
            //Join loop has alraedy been executed, ignore
            if (JoinLoopResult != null) return;

            JoinLoopResult = new();

            //There aren't any other members in the cluster besides ourseleves, so start a founder (leader).
            if (!Members.Any(x => !x.IsLocal))
            {
                JoinLoopResult.TrySetResult();
                return;
            }

            //One the cluster members we knew was a cluster leader who also knew us, so we just re-established the membership automatcially.
            if (Members.Any(x => x.IsLeader))
            {
                JoinLoopResult.TrySetResult();
                return;
            }

            //There are other members, but none of them automatically accepted us as member, so we have to ask to join them.
            _logger.LogDebug($"Attemping to join cluster with local endpoint {_environmentSettings.PublicEndpoint} ...");

            var joinRequest = new JoinClusterRequest
            {
                Id = _environmentSettings.Id,
                Endpoint = _environmentSettings.PublicEndpoint
            };

            while (!ShutDownSource.IsCancellationRequested)
            {
                //Ask each remote member
                foreach (var member in Members.Where(x => !x.IsLocal))
                {
                    if (ShutDownSource.IsCancellationRequested)
                    {
                        JoinLoopResult.TrySetCanceled();
                        return;
                    }

                    if (Members.Any(x => x.IsLeader))
                    {
                        JoinLoopResult.TrySetResult();
                        return;
                    }

                    _logger.LogDebug($"Requesting to join cluster via {member.Endpoint}.");

                    try
                    {
                        //Send join request
                        var joinResult = await _networkConnector.SendAsync(member, joinRequest, true, ShutDownSource.Token);

                        if (joinResult != null && joinResult.Success)
                        {
                            _logger.LogInformation("Successfully joined the cluster.");

                            JoinLoopResult.TrySetResult();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        //If the join request failed because the task was cancelled, terminate the loop
                        if (ShutDownSource.IsCancellationRequested)
                        {
                            JoinLoopResult.TrySetCanceled();
                            return;
                        }
                    }
                }

                _logger.LogDebug("Attempt failed. Trying again later ...");

                //All members rejected our join, try agian later, maybe one of them is then able to handle our request
                await Task.Delay(_environmentSettings.ConnectionTimeout);
            }

            //Join loop was canclled, 
            JoinLoopResult.TrySetCanceled();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await GracefulShutdownAsync(cancellationToken);
        }

        protected async Task RaftOnStopAsync()
        {
            await GracefulShutdownAsync();
        }

        public async Task RequestShutdownAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Shutting down ...");

            if (!_environmentSettings.IsTest) Environment.Exit(0);
        }

        protected async Task GracefulShutdownAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRunning || cancellationToken.IsCancellationRequested) return; //Already shut down

            _logger.LogDebug($"Attemping to gracefully leave the cluster ...");

            IsRunning = false;
            IsOperational = false;

            ShutDownSource.Cancel();

            //Wait for join loop to stop attempts if it has been running
            try
            {
                await (JoinLoopResult?.Task ?? Task.CompletedTask);
            }
            catch (OperationCanceledException)
            {
            }

            try
            {
                var leaveResult = await SendToLeaderAsync(new LeaveClusterRequest { Id = _environmentSettings.Id }, cancellationToken);

                if (leaveResult == null || !leaveResult.Success)
                {
                    _logger.LogInformation("Failed to gracefully leave the cluster! Manual removal might be required.");
                    return;
                }
            }
            catch (Exception)
            {
            }

            _logger.LogInformation("Successfully left the cluster.");

            await RequestShutdownAsync(cancellationToken);
        }

        protected async Task RaftOnMemberJoinedAsync(object? sender, IClusterMember clusterMember)
        {
            if (clusterMember == null) return;

            await HandleRaftMemberJoinedAsync(clusterMember);
        }

        protected async Task<ClusterMember> HandleRaftMemberJoinedAsync(IClusterMember clusterMember)
        {
            if (clusterMember == null || clusterMember.EndPoint == null) return default;

            //Subscribe to the member events
            clusterMember.MemberStatusChanged += async (args) => await OnRaftClusterMemberStatusChangeAsync(args);

            var endpoint = new Uri(clusterMember.EndPoint.ToString()!);

            bool isLocalMember = _environmentSettings.PublicEndpoint.Equals(endpoint);

            ClusterMember member;
            bool created = false;

            if (isLocalMember)
            {
                //Local join event
                member = new ClusterMember
                {
                    Id = _environmentSettings.Id,
                    Endpoint = endpoint,
                    IsLocal = true,
                    Tags = _environmentSettings.Tags,
                    Availability = ClusterMemberAvailability.Available
                };

                created = true;
            }
            else
            {
                //Remote member joined
                member = await GetMemberByEndpointAsync(endpoint, true);

                //Member did not exist yet create
                if (member == null)
                {
                    member = new ClusterMember
                    {
                        Endpoint = endpoint,
                        IsLocal = false,
                        Tags = new()
                    };

                    //Try to fetch the meta data already.
                    if (await RefreshMemberMetaDataAsync(clusterMember, member))
                    {
                        //Got a successful response so we know the member is able to handle incoming requests, hence he is available
                        member.Availability = ClusterMemberAvailability.Available;
                    }
                    else
                    {
                        //He was added to the cluster, but we were not able to reach him, so he is unavailable for now
                        member.Availability = ClusterMemberAvailability.Unavailable;
                    }

                    await _scopedMediator.Send(new CreateOrUpdateClusterMemberCmd
                    {
                        Data = member
                    });

                    created = true;
                }
            }

            //If the member was used from existing list, no need to fire any of the events or add him again
            if (!created) return member;

            Members.Add(member);

            if (!string.IsNullOrEmpty(member.Id))
            {
                //We know we joined locally, so only log other members coming in
                if (!isLocalMember) _logger.LogInformation($"Member(ID:{member.Id} | {member.Endpoint}) was added to the cluster.");

                OnMemberAdded?.Invoke(this, new(member));
            }
            else
            {
                _logger.LogInformation($"Member(EP:{member.Endpoint}) was added to the cluster but is not reachable. Events postponed until availability is confirmed.");
            }

            //Debounce member joins until the join loops checks if we are connected or else sends out join requests.
            //Give the cluster time for 2 repliction rounds to add our membership and replicate the data to us.
            JoinClusterDebouncer.Debounce(TimeSpan.FromMilliseconds(_environmentSettings.ConnectionTimeout * 2), JoinLoopAsync);

            return member;
        }

        protected async Task RaftOnMemberLeftAsync(object? sender, IClusterMember clusterMember)
        {
            if (clusterMember == null || clusterMember.EndPoint == null) return;

            var endpoint = new Uri(clusterMember.EndPoint.ToString()!);

            var member = await GetMemberByEndpointAsync(endpoint);

            if (member == null) return;

            if (!member.IsLocal)
            {
                await _scopedMediator.Send(new DeleteClusterMemberCmd
                {
                    ClusterMemberEndpoint = member.Endpoint
                });
            }

            Members.Remove(member);

            if (!string.IsNullOrEmpty(member.Id))
            {
                //No logging for local member leaving
                if (!_environmentSettings.PublicEndpoint.Equals(endpoint))
                {
                    _logger.LogInformation($"Member(ID:{member.Id} | {member.Endpoint}) was removed from the cluster.");
                }

                OnMemberRemoved?.Invoke(this, new(member));
            }
            else
            {
                _logger.LogInformation($"Member(EP:{member.Endpoint}) was removed from the cluster but was never reachable. No events were fired for this member.");
            }
        }

        protected async Task RaftOnLeaderChangedAsync(object? sender, IClusterMember clusterMember)
        {
            var previousLeader = Members.FirstOrDefault(x => x.IsLeader);

            if (previousLeader != null) previousLeader.IsLeader = false;

            if (clusterMember == null)
            {
                //We do not have a new leader anymore

                if (previousLeader != null)
                {
                    _logger.LogInformation($"Member(ID:{previousLeader.Id} | {previousLeader.Endpoint}) is no longer the cluster leader.");
                }

                //Broadcast no leader event
                OnClusterLeaderChanged?.Invoke(this, new(null!));

                //Switch to non operational mode
                if (IsOperational)
                {
                    IsOperational = false;

                    _logger.LogError($"Cluster is no longer operational.");
                }

                return;
            }
            else if (clusterMember.EndPoint == null)
            {
                //Broadcast no leader event
                OnClusterLeaderChanged?.Invoke(this, new(null!));

                //Switch to non operational mode
                if (IsOperational)
                {
                    IsOperational = false;

                    _logger.LogError($"Cluster is no longer operational.");
                }

                return; //No valid endpoint for non null member
            }

            var endpoint = new Uri(clusterMember.EndPoint.ToString()!);

            //If we do not know the new leader as member yet, add it!
            var newLeader = await GetMemberByEndpointAsync(endpoint) ?? await HandleRaftMemberJoinedAsync(clusterMember);

            //Update leader state
            newLeader.IsLeader = true;

            //Logging
            if (previousLeader == null)
            {
                _logger.LogInformation($"Member(ID:{newLeader.Id} | {newLeader.Endpoint}) is now the cluster leader.");
            }
            else
            {
                _logger.LogInformation($"Member(ID:{newLeader.Id} | {newLeader.Endpoint}) has replaced Member(ID:{previousLeader.Id} | {previousLeader.Endpoint}) as cluster leader.");
            }

            //Broadcast new leader
            OnClusterLeaderChanged?.Invoke(this, new(newLeader));

            //Due to dotNext implementation replication is not called on leader, so we need to invoke the events around it manually
            if (newLeader.IsLocal)
            {
                _ = Task.Run(() =>
                {
                    //If the cluster is created with just one node (cold start) we need to run code for inital setups like db defaults.
                    if (Members.Count == 1 && !InitializedDispatched)
                    {
                        InitializedDispatched = true;

                        OnInitialized?.Invoke(this, default!);
                    }

                    //Complete replication "with ourself"
                    RaftOnMessageQueueCleared();
                });
            }
        }

        protected async Task OnRaftClusterMemberStatusChangeAsync(DotNext.Net.Cluster.ClusterMemberStatusChangedEventArgs args)
        {
            var endpoint = new Uri(args.Member.EndPoint.ToString()!);

            var member = await GetMemberByEndpointAsync(endpoint);

            if (member == null) return;

            var oldId = member.Id;
            var oldAvailability = member.Availability;

            member.Availability = args.NewStatus switch
            {
                ClusterMemberStatus.Available => ClusterMemberAvailability.Available,
                ClusterMemberStatus.Unavailable => ClusterMemberAvailability.Unavailable,
                _ => ClusterMemberAvailability.Unknown
            };

            //Availability did not really change.
            if (oldAvailability == member.Availability) return;

            _logger.LogDebug($"Availability changed for Member(ID:{member.Id ?? "Unknown"} | {member.Endpoint}) from {oldAvailability} to {member.Availability}.");

            //If the member has become available re-fetch his info in case it changed.
            if (member.Availability == ClusterMemberAvailability.Available)
            {
                await RefreshMemberMetaDataAsync(args.Member, member);
            }

            if (!string.IsNullOrEmpty(oldId))
            {
                if (oldId == member.Id)
                {
                    //The member already had his id and it stayed the same, so he must have been reachable, so the join event was already fired. Just broadcast the status update for this member
                    OnMemberStatusChanged?.Invoke(this, new(member, oldAvailability, member.Availability));
                }
                else
                {
                    //Member id has changed, so we kick him out
                    Members.Remove(member);

                    OnMemberRemoved?.Invoke(this, new(member));

                    if (!string.IsNullOrEmpty(member.Id))
                    {
                        //We have a valid new id, so we add a new instance representing this member
                        var newMember = new ClusterMember
                        {
                            Id = member.Id,
                            Tags = member.Tags,
                            Endpoint = member.Endpoint,
                            Availability = member.Availability,
                            IsLeader = member.IsLeader,
                            IsLocal = member.IsLocal
                        };

                        Members.Add(newMember);

                        OnMemberAdded?.Invoke(this, new(newMember));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(member.Id))
            {
                //The member was known inside the cluster, but had no id. Now we have it, so we can broadcast his existence for the first time
                OnMemberAdded?.Invoke(this, new(member));
            }
        }

        protected void RaftOnReplicatedMessage(object? sender, MessageBase message)
        {
            _scopedMediator.Send(message).GetAwaiter().GetResult();

            OnReplicatedMessage?.Invoke(this, message);
        }

        protected void RaftOnMessageQueueCleared()
        {
            if (!IsOperational && HasLeader())
            {
                //Member is now operational and can process api requests
                IsOperational = true;

                _logger.LogInformation($"Cluster is now operational.");

                OnOperational?.Invoke(this, default!);
            }
        }

        protected static async Task<bool> RefreshMemberMetaDataAsync(IClusterMember raftMember, ClusterMember clusterMember)
        {
            try
            {
                var metaData = await raftMember.GetMetadataAsync(true);

                clusterMember.Id = metaData["memberid"];
                clusterMember.Tags = metaData["membertags"].Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

                return true;
            }
            catch
            {
            }

            return false;
        }

        protected async Task RefreshMetricsAsync(ClusterMember member, CancellationToken cancellationToken = default)
        {
            if (member.Availability != ClusterMemberAvailability.Available) return;

            try
            {
                var metrics = await SendAsync(member, new FetchMetricsCmd(), cancellationToken);

                if (metrics == null) return;

                member.AvgCpuLoad = metrics.CpuAvg;
            }
            catch
            {
            }
        }

        protected async Task RefreshMetricsAsync(CancellationToken cancellationToken = default)
        {
            var refreshTaks = new List<Task>();

            foreach (var member in Members.ToList())
            {
                refreshTaks.Add(RefreshMetricsAsync(member, cancellationToken));
            }

            await Task.WhenAll(refreshTaks);
        }

        public async Task<bool> AddMemberAsync(Uri endpoint, CancellationToken cancellationToken = default)
        {
            bool success = false;

            //Abort member login attempt after timeout window
            //Give some time to replicate data etc
            var timeout = _environmentSettings.ConnectionTimeout * 10;
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(timeout).Token).Token;

            try
            {
                await MembershipLock.WaitAsync(cancellationToken);

                success = await _raftService.AddMemberAsync(endpoint, cancellationToken);
            }
            catch (Exception ex)
            {
            }

            MembershipLock.Release();

            if (!success)
            {
                _logger.LogInformation($"New cluster member attempted to join but failed: {endpoint}");
            }

            return success;
        }

        public async Task<bool> RemoveMemberAsync(ClusterMember member, CancellationToken cancellationToken = default)
        {
            await MembershipLock.WaitAsync(cancellationToken);

            var success = await _raftService.RemoveMemberAsync(member.Endpoint, cancellationToken);

            if (success)
            {
                try
                {
                    await SendAsync(member, new ShutdownRequest(), cancellationToken);
                }
                catch
                {
                }
            }
            else
            {
                _logger.LogInformation($"Cluster member attempted to leave but failed: {member.Endpoint}");
            }

            MembershipLock.Release();

            return success;
        }

        public async Task<ReadOnlyCollection<ClusterMember>> GetMembersAsync(bool refreshData = false, CancellationToken cancellationToken = default)
        {
            if (refreshData) await RefreshMetricsAsync(cancellationToken);

            return Members.ToList().AsReadOnly();
        }

        public async Task<ClusterMember> GetMemberByIdAsync(string id, bool refreshData = false, CancellationToken cancellationToken = default)
        {
            var member = Members.FirstOrDefault(x => x.Id == id);

            if (member != null && refreshData) await RefreshMetricsAsync(member, cancellationToken);

            return member;
        }

        public async Task<ClusterMember> GetMemberByEndpointAsync(Uri endpoint, bool refreshData = false, CancellationToken cancellationToken = default)
        {
            var member = Members.FirstOrDefault(x => x.Endpoint.Equals(endpoint));

            if (member != null && refreshData) await RefreshMetricsAsync(member, cancellationToken);

            return member;
        }

        public async Task SendAsync(ClusterMember member, MessageBase message, CancellationToken cancellationToken = default)
        {
            await SendAsync<Unit>(member, message, cancellationToken);
        }

        public async Task<TResponse> SendAsync<TResponse>(ClusterMember member, RequestBase<TResponse> request, CancellationToken cancellationToken = default)
        {
            //Skip network connector if request is for us
            if (member.IsLocal)
            {
                return await _scopedMediator.Send(request, cancellationToken);
            }

            try
            {
                return await _networkConnector.SendAsync(member, request, cancellationToken: cancellationToken);
            }
            catch (Exception)
            {
            }

            throw new Exception("Unable to deliver message to target member.");
        }

        public async Task SendToLeaderAsync(MessageBase message, CancellationToken cancellationToken = default)
        {
            await SendToLeaderAsync<Unit>(message, cancellationToken);
        }

        public async Task<TResponse> SendToLeaderAsync<TResponse>(RequestBase<TResponse> request, CancellationToken cancellationToken = default)
        {
            //Skip network connector if request to leader is to ourselfs
            if (_raftService.IsLeader)
            {
                return await _scopedMediator.Send(request, cancellationToken);
            }

            //Send requests to any other member that we know as leader
            foreach (var member in Members.Where(x => !x.IsLocal && x.IsLeader).ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await _networkConnector.SendAsync(member, request, true, cancellationToken);
                }
                catch (Exception ex)
                {
                }
            }

            throw new LeaderUnavailableException("Unable to deliver message to cluster leader.");
        }

        /// <summary>
        /// Send a message that is send to all cluster members, but only guranteed to have been received by any future leader.
        /// Messages are handed out through the Mediator pipeline when received, so they can be handled anywhere in the application.
        /// The caller member will receive the message as well.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>True if the message was successfully replicated</returns>
        public async Task<bool> ReplicateAsync(MessageBase message, CancellationToken cancellationToken = default)
        {
            return await _raftService.ReplicateAsync(message, cancellationToken);
        }

        public bool HasLeader()
        {
            return Members.Any(x => x.IsLeader);
        }

        public bool IsLocalLeader()
        {
            return GetLocalMember()?.IsLeader ?? false;
        }

        public ClusterMember GetLocalMember()
        {
            return Members.FirstOrDefault(x => x.IsLocal);
        }

        protected class LeaderRedirection
        {
            private readonly RequestDelegate _next;

            public LeaderRedirection(RequestDelegate next)
            {
                _next = next;
            }

            public Task Redirect(HttpContext context)
            {
                if (context.Request.Headers.Any(x => x.Key.Equals("X-Redirect-Leader", StringComparison.OrdinalIgnoreCase)))
                {
                    var cluster = context.RequestServices.GetService<ClusterService>();
                    var leader = cluster.Members.FirstOrDefault(x => x.IsLeader);

                    if (leader is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        return Task.CompletedTask;
                    }

                    if (!leader.IsLocal)
                    {
                        var newUri = new Uri($"{leader.Endpoint.ToString().TrimEnd('/')}{context.Request.GetEncodedPathAndQuery()}");

                        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
                        context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Location] = newUri.AbsoluteUri;
                        return Task.CompletedTask;
                    }
                }

                return _next(context);
            }
        }

        public class ClusterOperationalMiddleware
        {
            private readonly RequestDelegate _next;

            public ClusterOperationalMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(HttpContext httpContext, ClusterService clusterService)
            {
                //Requests to the public facing api are blocked until the cluster becomes operational to serve read and write requests
                if (!clusterService.IsOperational && httpContext.Request.Path.StartsWithSegments(new("/api")))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }

                await _next(httpContext);
            }
        }
    }
}
