using MediatR;

namespace OpenStatusPage.Server.Application.Cluster.Communication
{
    /// <summary>
    /// Base type for requests sent in the cluster. Each request must return a response of type TResponse.
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class RequestBase<TResponse> : IRequest<TResponse>, IRequestBase
    {
    }
}
