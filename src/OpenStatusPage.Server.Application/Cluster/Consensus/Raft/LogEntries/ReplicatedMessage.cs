using DotNext.Net.Cluster.Consensus.Raft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Cluster.Consensus.Raft.LogEntries
{
    public class ReplicatedMessage : RaftLogEntryBase
    {
        public new const int TYPE = 100;

        public ReplicatedMessage(MessageBase message)
        {
            _message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public ReplicatedMessage(IRaftLogEntry entry, BinaryReader reader) : base(entry, reader)
        {
        }

        public override int? CommandId => TYPE;

        public MessageBase Message => _message;
        protected MessageBase _message;

        public override void WriteToBuffer(BinaryWriter writer)
        {
            base.WriteToBuffer(writer);

            writer.Write(JsonConvert.SerializeObject(Message, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                SerializationBinder = TypeNameSpawnRestrictions.Instance,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
        }

        public override void ReadFromBuffer(BinaryReader reader)
        {
            base.ReadFromBuffer(reader);

            _message = JsonConvert.DeserializeObject<MessageBase>(reader.ReadString(), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                SerializationBinder = TypeNameSpawnRestrictions.Instance,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            })!;
        }

        public class TypeNameSpawnRestrictions : DefaultSerializationBinder
        {
            public static readonly TypeNameSpawnRestrictions Instance = new();

            public override Type BindToType(string? assemblyName, string typeName)
            {
                if (!typeName.Contains("OpenStatusPage", StringComparison.OrdinalIgnoreCase) && //Not part of OSP namespace
                    !typeName.StartsWith("System.Collections", StringComparison.OrdinalIgnoreCase)) //Not part of a generic collection
                {
                    throw new JsonSerializationException($"Type {typeName} is not part of the supported namespace.");
                }

                return base.BindToType(assemblyName, typeName);
            }
        }
    }
}
