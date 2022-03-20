using MediatR;
using MediatR.Pipeline;

namespace OpenStatusPage.Server.Application.Misc.Mediator
{
    public interface IRequestPostProcessor<in TRequest> : IRequestPostProcessor<TRequest, Unit> where TRequest : IRequest<Unit>
    {
    }
}
