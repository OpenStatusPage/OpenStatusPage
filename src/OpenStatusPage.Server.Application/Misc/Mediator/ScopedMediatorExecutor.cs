using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Misc.Mediator
{
    public class ScopedMediatorExecutor : ISingletonService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ScopedMediatorExecutor(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006", Justification = "Match the MediatR function api names")]
        public async Task Send(MessageBase message, CancellationToken cancellationToken = default)
        {
            await Send<Unit>(message, cancellationToken);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006", Justification = "Match the MediatR function api names")]
        public async Task<TResponse> Send<TResponse>(RequestBase<TResponse> request, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            return await mediator.Send(request, cancellationToken);
        }
    }
}
