using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries;
using OpenStatusPage.Server.Application.Misc.Exceptions;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States
{
    public class PersistentMessageReplicatorState : DatabasePersistentState
    {
        private readonly ILogger<PersistentMessageReplicatorState> _logger;
        private readonly IServiceProvider _serviceProvider;

        protected Dictionary<long, Task<bool>> MessageDeliveryTasks { get; set; } = new();

        public event EventHandler<ReplicatedMessage> OnMessageReceived;

        public PersistentMessageReplicatorState(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = serviceProvider.GetRequiredService<ILogger<PersistentMessageReplicatorState>>();
            _serviceProvider = serviceProvider;
        }

        protected override async ValueTask<IRaftLogEntry> ParseLogEntryAsync<TEntryImpl>(TEntryImpl entry, BinaryReader reader, CancellationToken token = default)
        {
            //Handle data snapshots
            if (entry.IsSnapshot)
            {
                return new ReplicatedMessage(entry, reader);
            }

            switch (entry.CommandId)
            {
                case ReplicatedMessage.TYPE:
                {
                    return new ReplicatedMessage(entry, reader);
                }
            }

            return await base.ParseLogEntryAsync(entry, reader, token);
        }

        protected override async Task<(IRaftLogEntry Entry, long Index)> ProcessSnapshotAsync(IRaftLogEntry snapshot, long? endIndex, CancellationToken token = default)
        {
            var snapshotData = await DataSnapshotCmd.BuildAsync(_serviceProvider, token);

            if (snapshotData.DataConstructionMessages.Any(x => x.Value.Count > 0))
            {
                snapshot = new ReplicatedMessage(snapshotData)
                {
                    IsSnapshot = true,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            return await base.ProcessSnapshotAsync(snapshot, endIndex, token);
        }

        protected override async ValueTask<bool> AppendEntryAsync<TEntryImpl>(TEntryImpl entry, long index, bool skipCommitted = false, CancellationToken token = default)
        {
            //As leader send out the message eventhandler on append. If this throws the replication will not take place to followers and the error can be handled by the caller.
            if ((IsLeader || entry.IsSnapshot) && entry is ReplicatedMessage replicatedMessage)
            {
                _logger.LogDebug($"AppendEntryAsync(IsLeader:{IsLeader}) -> Delivering replicated Message {replicatedMessage.Message.GetType().Name}...");

                OnMessageReceived?.Invoke(this, replicatedMessage);
            }

            return await base.AppendEntryAsync(entry, index, skipCommitted, token);
        }

        protected override async Task<bool> CommitEntryAsync(IRaftLogEntry entry, long index, CancellationToken token = default)
        {
            //Leader must have already processed this message during his append phase, so skip it for him
            if (!IsLeader && entry is ReplicatedMessage replicatedMessage)
            {
                if (MessageDeliveryTasks.TryGetValue(index, out var deliveryTask))
                {
                    if (!deliveryTask.IsCompleted || !deliveryTask.Result)
                    {
                        //The delivery is already being executed but has not succeeded yet.
                        // <or>
                        //The delivery has been executed but the result was not successful, so it must be attempted again
                        MessageDeliveryTasks.Remove(index);

                        return false;
                    }
                }
                else
                {
                    MessageDeliveryTasks[index] = Task.Run(() =>
                    {
                        try
                        {
                            _logger.LogDebug($"CommitEntryAsync(IsLeader:{IsLeader}) -> Delivering replicated Message {replicatedMessage.Message.GetType().Name}...");

                            OnMessageReceived?.Invoke(this, replicatedMessage);
                        }
                        catch (FinalFailureException ex)
                        {
                            //Treat this entry as committed, since this type of exception would fail again and again
                            //The state change will simply not have happend here. Normally this case should never arise if the leader successfuly executed this.
                            _logger.LogDebug($"CommitEntryAsync(IsLeader:{IsLeader}) -> FinalFailureException: {ex.Message}\n{ex.StackTrace}");
                        }
                        catch (TemporaryFailureException ex)
                        {
                            //The action has failed this time, but could be attempted again later. So we reject the commit for now and await another replication round
                            _logger.LogDebug($"CommitEntryAsync(IsLeader:{IsLeader}) -> TemporaryFailureException: {ex.Message}\n{ex.StackTrace}");
                            return false;
                        }
                        catch (Exception ex)
                        {
                            //Any kind of unknown exception will be treated as a problem that might go away by itself again
                            //Proper validation and execution success requirement on the leader should ensure that there is no flawed request with things like NullReference or DivideByZero etc.
                            _logger.LogDebug($"CommitEntryAsync(IsLeader:{IsLeader}) -> Exception({ex.GetType().Name}): {ex.Message}\n{ex.StackTrace}");
                            return false;
                        }

                        return true;
                    });

                    return false;
                }
            }

            return await base.CommitEntryAsync(entry, index, token);
        }

        public override async ValueTask<bool> IsCompactionRequiredAsync(long endIndex, CancellationToken token = default)
        {
            if (Log.Count > 100) return true;

            return await base.IsCompactionRequiredAsync(endIndex, token);
        }
    }
}
