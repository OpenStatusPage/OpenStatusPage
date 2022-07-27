using DotNext.Net.Cluster.Consensus.Raft;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries;
using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Server.Persistence;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.States
{
    public class DatabasePersistentState : InMemoryDictionaryState
    {
        private readonly IServiceProvider _serviceProvider;

        public DatabasePersistentState(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task<(IRaftLogEntry Entry, long Index)> ProcessSnapshotAsync(IRaftLogEntry snapshot, long? endIndex, CancellationToken token = default)
        {
            if (snapshot is RaftLogEntryBase raftLogEntry)
            {
                using var scope = _serviceProvider.CreateScope();
                var _applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                //Fetch data for specific log entry
                if (endIndex.HasValue)
                {
                    var endEntry = await _applicationDbContext.RaftLogMetaEntries
                        .FirstOrDefaultAsync(x => x.Index == endIndex, token);

                    if (endEntry != null)
                    {
                        raftLogEntry.Term = endEntry.Term;
                    }

                    return (raftLogEntry, endIndex.Value);
                }

                //Build inital snapshot, as we do not have an endIndex specified
                var latestEntry = await _applicationDbContext.RaftLogMetaEntries
                    .OrderByDescending(x => x.Index)
                    .FirstOrDefaultAsync(token);

                //Set term from db so the one who knows the most recent data will be elected leader in the inital election
                raftLogEntry.Term = latestEntry?.Term ?? 1;

                return (raftLogEntry, latestEntry?.Index ?? 1);
            }

            return await base.ProcessSnapshotAsync(snapshot, endIndex, token);
        }

        public override async ValueTask CompactLogAsync(long endIndex, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var _applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //Get all log entries before the last one that remains
            var staleEntries = await _applicationDbContext.RaftLogMetaEntries
                .Where(x => x.Index < endIndex)
                .ToListAsync(token);

            try
            {
                _applicationDbContext.RemoveRange(staleEntries);
                await _applicationDbContext.SaveChangesAsync(token);
            }
            catch
            {
            }

            await base.CompactLogAsync(endIndex, token);
        }

        protected async Task PersistCommitIndexAsync(IRaftLogEntry entry, long index, CancellationToken token = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var _applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            //Check if this index was already marked as commited in the db (can happen if two raft logs share the same db)
            if (!_applicationDbContext.RaftLogMetaEntries.Any(x => x.Index == index))
            {
                try
                {
                    _applicationDbContext.Add(new RaftLogMetaEntry
                    {
                        Index = index,
                        Term = entry.Term
                    });

                    await _applicationDbContext.SaveChangesAsync(token);
                }
                catch
                {
                    //Catch in case the raft log entry was already added in a shared db
                }
            }
        }
    }
}
