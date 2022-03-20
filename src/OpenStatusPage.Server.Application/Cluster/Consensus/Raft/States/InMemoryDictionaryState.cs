using DotNext.Net.Cluster.Consensus.Raft;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States
{
    public class InMemoryDictionaryState : RaftStateBase
    {
        protected Dictionary<long, IRaftLogEntry> Log { get; set; } = new();

        protected long? SnapshotIndex { get; set; }

        public override async Task InitializeAsync(CancellationToken token = default)
        {
            var initalSnapshot = await ProcessSnapshotAsync(null!, null!, token);

            if (initalSnapshot != default && initalSnapshot.Entry != null)
            {
                Log[initalSnapshot.Index] = initalSnapshot.Entry;
                SnapshotIndex = initalSnapshot.Index;
            }

            //Let the base implementation handle the rest. It will read the snapshot and update all the state variables based on it
            await base.InitializeAsync(token);
        }

        protected virtual async Task<(IRaftLogEntry Entry, long Index)> ProcessSnapshotAsync(IRaftLogEntry snapshot, long? endIndex, CancellationToken token = default)
        {
            return default;
        }

        protected override async ValueTask<bool> AppendEntryAsync<TEntryImpl>(TEntryImpl entry, long index, bool skipCommitted = false, CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return false;

            //A snapshot will wipe everything and just keep the snapshot
            if (entry.IsSnapshot)
            {
                Log.Clear();
                SnapshotIndex = index;
            }

            Log[index] = entry;

            //Console.WriteLine($"Added entry {entry.GetType().Name} at index {index}.");

            return true;
        }

        protected override async ValueTask<(long, long)> CommitEntriesAsync(long startIndex, long endIndex, CancellationToken token = default)
        {
            long commitedEntries = 0;
            long lastTerm = 0;

            for (var index = startIndex; index <= endIndex; index++)
            {
                if (token.IsCancellationRequested) break;

                if (Log.TryGetValue(index, out var entry) && await CommitEntryAsync(entry, index, token))
                {
                    lastTerm = entry.Term;
                    commitedEntries++;

                    //Console.WriteLine($"Commited index {index}.");
                }
                else
                {
                    //Unable to get the entry to commit. Abort.
                    break;
                }
            }

            return (commitedEntries, lastTerm);
        }

        protected virtual async Task<bool> CommitEntryAsync(IRaftLogEntry entry, long index, CancellationToken token = default)
        {
            return true;
        }

        public async override ValueTask DropEntriesAsync(long startIndex, long count, CancellationToken token = default)
        {
            for (var nDrop = 0L; nDrop < count; nDrop++)
            {
                Log.Remove(startIndex + nDrop);
            }
        }

        protected override ValueTask<(TResult Result, long ResultIndex)> ReadEntryAsync<TResult>(long index, CancellationToken token = default)
        {
            //Any attempt to read an index lower or equal to the last snapshot will result in the snapshot being returned
            if (SnapshotIndex.HasValue) index = Math.Max(index, SnapshotIndex.Value);

            if (Log.TryGetValue(index, out var value))
            {
                return ValueTask.FromResult(((TResult)value, index));
            }

            return default;
        }

        public override async ValueTask CompactLogAsync(long endIndex, CancellationToken token = default)
        {
            var snapshot = await ProcessSnapshotAsync(null!, endIndex, token);

            if (snapshot != default && snapshot.Entry != null)
            {
                Log[endIndex] = snapshot.Entry;
            }

            //Update snapshot index
            SnapshotIndex = endIndex;

            //Drop everything except the end index, as that is our snapshot
            Log.Keys
               .Where(x => x < endIndex)
               .ToList()
               .ForEach(x => Log.Remove(x));
        }
    }
}
