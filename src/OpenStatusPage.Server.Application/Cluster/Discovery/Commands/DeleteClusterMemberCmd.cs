using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc.Attributes;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    [RequiresDbTransaction]
    public class DeleteClusterMemberCmd : MessageBase
    {
        public Uri ClusterMemberEndpoint { get; set; }

        public class Handler : IRequestHandler<DeleteClusterMemberCmd>
        {
            private readonly ClusterMemberService _clusterMemberService;

            public Handler(ClusterMemberService clusterMemberService)
            {
                _clusterMemberService = clusterMemberService;
            }

            public async Task<Unit> Handle(DeleteClusterMemberCmd request, CancellationToken cancellationToken)
            {
                var clusterMember = await _clusterMemberService.Get(request.ClusterMemberEndpoint).FirstOrDefaultAsync(cancellationToken);

                //Already deleted
                if (clusterMember == null) return Unit.Value;

                await _clusterMemberService.DeleteAsync(clusterMember);

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<DeleteClusterMemberCmd>
        {
            public Validator()
            {
                RuleFor(x => x.ClusterMemberEndpoint)
                    .NotEmpty()
                    .WithMessage("Missing ClusterMemberEndpoint.");
            }
        }
    }
}
