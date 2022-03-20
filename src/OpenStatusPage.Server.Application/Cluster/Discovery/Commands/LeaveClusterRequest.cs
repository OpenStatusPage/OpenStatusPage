using FluentValidation;
using MediatR;
using OpenStatusPage.Server.Application.Cluster.Communication;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    public class LeaveClusterRequest : RequestBase<LeaveClusterRequest.Response>
    {
        public string Id { get; set; }

        public class Handler : IRequestHandler<LeaveClusterRequest, Response>
        {
            private readonly ClusterService _clusterService;

            public Handler(ClusterService clusterService)
            {
                _clusterService = clusterService;
            }

            public async Task<Response> Handle(LeaveClusterRequest request, CancellationToken cancellationToken)
            {
                //If we are not the cluster leader, reject the leave request. Member will need to contact the leader directly
                if (!_clusterService.IsLocalLeader()) return new();

                var member = await _clusterService.GetMemberByIdAsync(request.Id, cancellationToken: cancellationToken);

                return new Response
                {
                    Success = member != null && await _clusterService.RemoveMemberAsync(member, cancellationToken)
                };
            }
        }

        public class Response
        {
            public bool Success { get; set; }
        }

        public class Validator : AbstractValidator<LeaveClusterRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty()
                    .WithMessage("Id was not specified.");
            }
        }
    }
}
