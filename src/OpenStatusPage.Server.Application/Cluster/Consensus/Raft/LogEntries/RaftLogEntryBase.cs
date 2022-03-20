using DotNext.IO;
using DotNext.Net.Cluster.Consensus.Raft;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries
{
    public class RaftLogEntryBase : IRaftLogEntry
    {
        public const int TYPE = 0;

        public RaftLogEntryBase()
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public RaftLogEntryBase(IRaftLogEntry entry, BinaryReader reader)
        {
            Term = entry.Term;
            Timestamp = entry.Timestamp;
            IsSnapshot = entry.IsSnapshot;

            ReadFromBuffer(reader);
        }

        public long Term { get; set; }

        public bool IsSnapshot { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public bool IsReusable => true;

        public long? Length => null;

        public virtual int? CommandId => TYPE;

        public async ValueTask WriteToAsync<TWriter>(TWriter writer, CancellationToken token) where TWriter : IAsyncBinaryWriter
        {
            var memory = new MemoryStream();

            WriteToBuffer(new BinaryWriter(memory));

            await writer.WriteAsync(memory.GetBuffer(), token: token);
        }

        public virtual void WriteToBuffer(BinaryWriter writer)
        {
        }

        public virtual void ReadFromBuffer(BinaryReader reader)
        {
        }
    }
}
