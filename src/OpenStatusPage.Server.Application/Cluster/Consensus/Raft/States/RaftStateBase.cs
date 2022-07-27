using DotNext.IO;
using DotNext.IO.Log;
using DotNext.Net.Cluster;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.Threading;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries;

using BoxedClusterMemberId = DotNext.Runtime.BoxedValue<DotNext.Net.Cluster.ClusterMemberId>;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States
{
    public class RaftStateBase : IPersistentState, ILogCompactionSupport, IDisposable
    {
        public bool IsLeader { get; set; }

        /// <summary>
        /// Parse concrete log entry instace from the wrapper entry and the binary reader data
        /// </summary>
        /// <typeparam name="TEntryImpl"></typeparam>
        /// <param name="entry"></param>
        /// <param name="reader"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        protected virtual async ValueTask<IRaftLogEntry> ParseLogEntryAsync<TEntryImpl>(TEntryImpl entry, BinaryReader reader, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            return entry;
        }

        /// <summary>
        /// Append the entry at the given index. Throw exception if that index was already taken by a commited entry - unless skipCommitted == true
        /// </summary>
        /// <typeparam name="TEntryImpl"></typeparam>
        /// <param name="entry"></param>
        /// <param name="index"></param>
        /// <param name="skipCommitted"></param>
        /// <param name="token"></param>
        protected virtual async ValueTask<bool> AppendEntryAsync<TEntryImpl>(TEntryImpl entry, long index, bool skipCommitted = false, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Commit all entries in the log in the given index range
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="token"></param>
        /// <returns>How many entires were commited + term of the last commited entry</returns>
        protected virtual async ValueTask<(long, long)> CommitEntriesAsync(long startIndex, long endIndex, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Drop n(count) entries starting from startIndex 
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="token"></param>
        public virtual async ValueTask DropEntriesAsync(long startIndex, long count, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read the log entry at the given index
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="index"></param>
        /// <param name="token"></param>
        /// <returns>Tuple with result value and the index it represents (for returning snapshots)</returns>
        protected virtual async ValueTask<(TResult Result, long ResultIndex)> ReadEntryAsync<TResult>(long index, CancellationToken token = default) where TResult : IRaftLogEntry
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determine if there is the need for a log compaction up to including endIndex.
        /// </summary>
        /// <param name="endIndex"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public virtual async ValueTask<bool> IsCompactionRequiredAsync(long endIndex, CancellationToken token = default)
        {
            return false;
        }

        /// <summary>
        /// Compact all log entries including the end index into a single snapshot representing the state
        /// </summary>
        /// <typeparam name="TEntryImpl"></typeparam>
        /// <param name="endIndex">Inclusive index of how far the log shall be compacted</param>
        /// <param name="token"></param>
        public virtual async ValueTask CompactLogAsync(long endIndex, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        #region INTERFACE_IMPLEMENTATION

        private readonly AsyncReaderWriterLock _accessLock = new();
        private readonly AsyncManualResetEvent _commitEvent = new(false);

        private long _highestLogTerm, _highestLogIndex, _commitedLogIndex;

        public long LastCommittedEntryIndex => _commitedLogIndex.VolatileRead();

        public long LastUncommittedEntryIndex => _highestLogIndex.VolatileRead();

        public virtual async Task InitializeAsync(CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return;

            var startupData = await ReadAsync(new LogInitReader(), 0, token);

            if (startupData != default && startupData.Index.HasValue)
            {
                _term.VolatileWrite(startupData.Term);
                _highestLogTerm.VolatileWrite(startupData.Term);
                _highestLogIndex.VolatileWrite(startupData.Index.Value);
                _commitedLogIndex.VolatileWrite(startupData.Index.Value);
            }
        }

        public async ValueTask AppendAsync<TEntryImpl>(ILogEntryProducer<TEntryImpl> entries, long startIndex, bool skipCommitted = false, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            await AppendMultipleHelperAsync(entries, startIndex, skipCommitted, token);
        }

        public async ValueTask<long> AppendAsync<TEntryImpl>(ILogEntryProducer<TEntryImpl> entries, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            return await AppendMultipleHelperAsync(entries, null, false, token);
        }

        private async ValueTask<long> AppendMultipleHelperAsync<TEntryImpl>(ILogEntryProducer<TEntryImpl> entries, long? startIndex, bool skipCommitted = false, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            using (await _accessLock.AcquireWriteLockAsync(token).ConfigureAwait(false))
            {
                startIndex ??= _highestLogIndex.VolatileRead() + 1L;

                long? resultIndex = null;

                while (entries.RemainingCount > 0)
                {
                    if (token.IsCancellationRequested) break;

                    await entries.MoveNextAsync().ConfigureAwait(false);

                    if (!await AppendHelperAsync(entries.Current, startIndex.Value, skipCommitted, token).ConfigureAwait(false)) break;

                    if (!resultIndex.HasValue) resultIndex = startIndex.Value;

                    startIndex++;
                }

                return resultIndex ?? 0;
            }
        }

        public async ValueTask AppendAsync<TEntryImpl>(TEntryImpl entry, long startIndex, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            using (await _accessLock.AcquireWriteLockAsync(token).ConfigureAwait(false))
            {
                await AppendHelperAsync(entry, startIndex, false, token).ConfigureAwait(false);
            }
        }

        private async ValueTask<bool> AppendHelperAsync<TEntryImpl>(TEntryImpl entry, long startIndex, bool skipCommitted = false, CancellationToken token = default) where TEntryImpl : notnull, IRaftLogEntry
        {
            if (startIndex <= _commitedLogIndex.VolatileRead())
            {
                if (!skipCommitted) throw new InvalidOperationException("Tried to append entry in an already commited range.");

                return true;
            }

            if (!entry.IsSnapshot && startIndex > _highestLogIndex.VolatileRead() + 1L)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            IRaftLogEntry concreteEntry;

            if (entry is RaftLogEntryBase)
            {
                //Already concrete
                concreteEntry = entry;
            }
            else
            {
                var bytes = await entry.ToByteArrayAsync(token: token);

                if (bytes != null && bytes.Length > 0)
                {
                    //Non empty entry to parse
                    concreteEntry = await ParseLogEntryAsync(entry, new BinaryReader(new MemoryStream(bytes)), token);
                }
                else
                {
                    //Empty entry, just use as it is
                    concreteEntry = entry;
                }
            }

            if (!await AppendEntryAsync(concreteEntry, startIndex, false, token).ConfigureAwait(false)) return false;

            if (entry.IsSnapshot)
            {
                //A snapshot must be received first in order, and it wipes any state that came before it.
                _highestLogTerm.VolatileWrite(concreteEntry.Term);
                _highestLogIndex.VolatileWrite(startIndex);
                _commitedLogIndex.VolatileWrite(startIndex);
                _commitEvent.Set(true);
            }
            else
            {
                //Increment index for latest log entry appended
                _highestLogIndex.VolatileWrite(startIndex);
            }

            return true;
        }

        public ValueTask<long> CommitAsync(CancellationToken token = default)
        {
            return CommitHelperAsync(null, token);
        }

        public ValueTask<long> CommitAsync(long endIndex, CancellationToken token = default)
        {
            return CommitHelperAsync(endIndex, token);
        }

        private async ValueTask<long> CommitHelperAsync(long? endIndex, CancellationToken token)
        {
            using (await _accessLock.AcquireWriteLockAsync(token).ConfigureAwait(false))
            {
                var commited = _commitedLogIndex.VolatileRead();
                endIndex ??= _highestLogIndex.VolatileRead();

                //The inclusive end index is already commited, so nothing we need to do
                if (endIndex == commited) return 0;

                var startIndex = commited + 1L;

                var result = await CommitEntriesAsync(startIndex, endIndex.Value, token);

                //If we commited any values ...
                if (result.Item1 > 0)
                {
                    //... update the commit index
                    _commitedLogIndex.VolatileWrite(startIndex + result.Item1 - 1L);

                    //... increment the highest term
                    _highestLogTerm.VolatileWrite(result.Item2);

                    if (await IsCompactionRequiredAsync(LastCommittedEntryIndex, token))
                    {
                        await CompactLogAsync(LastCommittedEntryIndex, token);
                    }

                    _commitEvent.Set(true);
                }

                return result.Item1;
            }
        }

        public async ValueTask<long> DropAsync(long startIndex, CancellationToken token = default)
        {
            using (await _accessLock.AcquireWriteLockAsync(token).ConfigureAwait(false))
            {
                if (startIndex <= _commitedLogIndex.VolatileRead()) throw new InvalidOperationException("Tried to append entry in an already commited range.");

                var count = _highestLogIndex.VolatileRead() - startIndex + 1L;

                await DropEntriesAsync(startIndex, count, token);

                //Move index back to the last log entry before the drop range
                _highestLogIndex.VolatileWrite(startIndex - 1L);

                return count;
            }
        }

        public ValueTask<TResult> ReadAsync<TResult>(ILogEntryConsumer<IRaftLogEntry, TResult> reader, long startIndex, CancellationToken token = default)
        {
            return ReadHelperAsync(reader, startIndex, null, token);
        }

        public ValueTask<TResult> ReadAsync<TResult>(ILogEntryConsumer<IRaftLogEntry, TResult> reader, long startIndex, long endIndex, CancellationToken token = default)
        {
            return ReadHelperAsync(reader, startIndex, endIndex, token);
        }

        private async ValueTask<TResult> ReadHelperAsync<TResult>(ILogEntryConsumer<IRaftLogEntry, TResult> reader, long startIndex, long? endIndex, CancellationToken token = default)
        {
            using (await _accessLock.AcquireReadLockAsync(token).ConfigureAwait(false))
            {
                endIndex ??= _highestLogIndex.VolatileRead();

                if (endIndex > _highestLogIndex.VolatileRead()) throw new ArgumentOutOfRangeException(nameof(endIndex));

                var entries = new List<IRaftLogEntry>();

                long? snapShotIndex = null;

                for (var index = startIndex; index <= endIndex; index++)
                {
                    (var readResult, var resultIndex) = await ReadEntryAsync<IRaftLogEntry>(index, token).ConfigureAwait(false);

                    if (readResult != null)
                    {
                        entries.Add(readResult);

                        if (readResult.IsSnapshot)
                        {
                            //e.g requested log index 2, but everything up to 500 was compacted into a snapshot at index 500, so we return 500 to let the higher logic skip there
                            index = resultIndex;

                            if (!snapShotIndex.HasValue)
                            {
                                snapShotIndex = resultIndex;
                            }
                            else
                            {
                                throw new Exception("Attempted to supply more than one snapshot.");
                            }
                        }
                    }
                }

                return await reader.ReadAsync<IRaftLogEntry, IRaftLogEntry[]>(entries.ToArray(), snapShotIndex, token).ConfigureAwait(false);
            }
        }

        public ValueTask WaitForCommitAsync(CancellationToken token = default)
        {
            return _commitEvent.WaitAsync(token);
        }

        public async ValueTask WaitForCommitAsync(long index, CancellationToken token = default)
        {
            while (index > _commitedLogIndex.VolatileRead())
            {
                await _commitEvent.WaitAsync(token).ConfigureAwait(false);
            }
        }

        public async ValueTask ForceCompactionAsync(CancellationToken token = default)
        {
            using (await _accessLock.AcquireWriteLockAsync(token).ConfigureAwait(false))
            {
                await CompactLogAsync(LastCommittedEntryIndex, token);
            }
        }

        public void Dispose()
        {
            _accessLock.Dispose();
            _commitEvent.Dispose();
        }

        #endregion

        #region LEADER_ELECTION

        private long _term;

        private volatile BoxedClusterMemberId? _lastVote;

        public long Term => _term.VolatileRead();

        public bool IsVotedFor(in ClusterMemberId id)
        {
            return _lastVote is null || _lastVote.Value == id;
        }

        public ValueTask<long> IncrementTermAsync()
        {
            return new(_term.IncrementAndGet());
        }

        public ValueTask<long> IncrementTermAsync(ClusterMemberId member)
        {
            _lastVote = BoxedClusterMemberId.Box(member);
            return new(_term.IncrementAndGet());
        }

        public ValueTask UpdateTermAsync(long term, bool resetLastVote)
        {
            _term.VolatileWrite(term);

            if (resetLastVote) _lastVote = null;

            return new();
        }

        public ValueTask UpdateVotedForAsync(ClusterMemberId id)
        {
            _lastVote = BoxedClusterMemberId.Box(id);

            return new();
        }

        public async ValueTask EnsureConsistencyAsync(CancellationToken token = default)
        {
            while (_term.VolatileRead() != _highestLogTerm.VolatileRead())
            {
                await _commitEvent.WaitAsync(token).ConfigureAwait(false);
            }
        }

        #endregion

        protected class LogInitReader : ILogEntryConsumer<IRaftLogEntry, (long Term, long? Index)>
        {
            ValueTask<(long Term, long? Index)> ILogEntryConsumer<IRaftLogEntry, (long Term, long? Index)>.ReadAsync<TEntryImpl, TList>(TList entries, long? snapshotIndex, CancellationToken token)
            {
                return entries.Count > 0 ? new((entries[0].Term, snapshotIndex)) : default;
            }
        }
    }
}
