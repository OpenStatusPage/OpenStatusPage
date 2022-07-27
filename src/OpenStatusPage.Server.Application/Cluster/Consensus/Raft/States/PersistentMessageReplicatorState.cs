using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries;
using System.Collections.Concurrent;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States
{
    public class PersistentMessageReplicatorState : DatabasePersistentState
    {
        private readonly ILogger<PersistentMessageReplicatorState> _logger;
        private readonly IServiceProvider _serviceProvider;

        protected ConcurrentQueue<(ReplicatedMessage Message, long Index)> _incomingMessages = new();
        protected Task _incomingMessagesHandler;

        public event EventHandler<ReplicatedMessage> OnMessageReceived;

        public event EventHandler OnMessageQueueCleared;

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
                if (IsLeader)
                {
                    try
                    {
                        _logger.LogDebug($"AppendEntryAsync(IsLeader:{IsLeader}) -> Delivering replicated Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()})...");

                        //Invoke directly to throw any exceptions during command processing into the leader caller context.
                        OnMessageReceived?.Invoke(this, replicatedMessage);

                        _logger.LogDebug($"AppendEntryAsync(IsLeader:{IsLeader}) -> Replicated Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()}) successfully delivered.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AppendEntryAsync(IsLeader:{IsLeader}) -> Exception({ex.GetType().Name}) for Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()}): {ex.Message}\n{ex.StackTrace}");

                        throw; //Throw to report failure to leader caller context that tried to replicate the command
                    }
                }
                else
                {
                    ProcessMessageBuffered(replicatedMessage, index);
                }
            }

            return await base.AppendEntryAsync(entry, index, skipCommitted, token);
        }

        protected override async Task<bool> CommitEntryAsync(IRaftLogEntry entry, long index, CancellationToken token = default)
        {
            if (entry is ReplicatedMessage replicatedMessage)
            {
                if (IsLeader)
                {
                    //Commit index to db on leader, message was already dispatched during append phase
                    await PersistCommitIndexAsync(entry, index, token);
                }
                else
                {
                    ProcessMessageBuffered(replicatedMessage, index);
                }
            }

            return await base.CommitEntryAsync(entry, index, token);
        }

        public override async ValueTask<bool> IsCompactionRequiredAsync(long endIndex, CancellationToken token = default)
        {
            if (Log.Count > 100) return true;

            return await base.IsCompactionRequiredAsync(endIndex, token);
        }

        public bool HasBufferedMessages()
        {
            return _incomingMessagesHandler is { IsCompleted: false };
        }

        protected void ProcessMessageBuffered(ReplicatedMessage message, long index)
        {
            _incomingMessages.Enqueue((message, index));

            //A message handler is still running, we can return after submitting it to the queue.
            if (HasBufferedMessages()) return;

            _incomingMessagesHandler = Task.Run(async () =>
            {
                while (_incomingMessages.TryDequeue(out var incoming))
                {
                    var replicatedMessage = incoming.Message;

                    try
                    {
                        _logger.LogDebug($"ProcessMessageBuffered() -> Delivering replicated Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()})...");

                        OnMessageReceived?.Invoke(this, replicatedMessage);

                        _logger.LogDebug($"ProcessMessageBuffered() -> Replicated Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()}) successfully delivered.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"ProcessMessageBuffered() -> Exception({ex.GetType().Name}) for Message({replicatedMessage.Message.GetType().Name}|{replicatedMessage.Message.GetHashCode()}): {ex.Message}\n{ex.StackTrace}");
                    }

                    await PersistCommitIndexAsync(replicatedMessage, incoming.Index);
                }

                OnMessageQueueCleared.Invoke(this, null!);

                _incomingMessagesHandler = null!;
            });
        }
    }
}
