using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Cluster.Consensus
{
    public interface ISnapshotDataProvider : ITransientService
    {
        Task<List<MessageBase>> GetDataAsync(CancellationToken cancellationToken = default);

        Task ApplyDataAsync(List<MessageBase> data, CancellationToken cancellationToken = default);
    }
}
