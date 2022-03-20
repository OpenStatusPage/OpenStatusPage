using FluentValidation;
using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    public class JoinClusterRequest : RequestBase<JoinClusterRequest.Response>
    {
        public Uri Endpoint { get; set; }

        public string Id { get; set; }

        public class Handler : IRequestHandler<JoinClusterRequest, Response>
        {
            private readonly ClusterService _clusterService;

            public Handler(ClusterService clusterService)
            {
                _clusterService = clusterService;
            }

            public async Task<Response> Handle(JoinClusterRequest request, CancellationToken cancellationToken)
            {
                //If we are not the leader, and sombody asks to join the cluster, forward his request to the leader
                if (!_clusterService.IsLocalLeader())
                {
                    try
                    {
                        return await _clusterService.SendToLeaderAsync(request, cancellationToken);
                    }
                    catch
                    {
                        return new(); //Return success false response if it failed
                    }
                }

                var success = true;

                if (await _clusterService.GetMemberByIdAsync(request.Id, cancellationToken: cancellationToken) == null)
                {
                    success = await _clusterService.AddMemberAsync(request.Endpoint, cancellationToken);
                }

                return new Response
                {
                    Success = success
                };
            }
        }

        public class Response
        {
            public bool Success { get; set; }
        }

        public class Validator : AbstractValidator<JoinClusterRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Endpoint)
                    .NotEmpty()
                    .WithMessage("Endpoint was not specified.");

                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Id was not specified.");
            }
        }
    }
}
