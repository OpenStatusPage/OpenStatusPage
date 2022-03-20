using MediatR;

namespace OpenStatusPage.Server.Application.Cluster.Communication
{
    /// <summary>
    /// Base type for one way messages sent in the cluster. Though delivery is guaranteed, the response data is empty.
    /// </summary>
    public abstract class MessageBase : RequestBase<Unit>
    {
    }
}
