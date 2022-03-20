using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Communication
{
    public interface INetworkConnector
    {
        Task<TResponse> SendAsync<TResponse>(ClusterMember node, RequestBase<TResponse> request, bool redirectToLeader = false, CancellationToken cancellationToken = default);
    }
}
