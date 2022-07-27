using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    public class CreateOrUpdateClusterMemberCmd : MessageBase
    {
        public ClusterMember Data { get; set; }

        public class Handler : IRequestHandler<CreateOrUpdateClusterMemberCmd>
        {
            private readonly ClusterMemberService _clusterMemberService;

            public Handler(ClusterMemberService clusterMemberService)
            {
                _clusterMemberService = clusterMemberService;
            }

            public async Task<Unit> Handle(CreateOrUpdateClusterMemberCmd request, CancellationToken cancellationToken)
            {
                var clusterMember = await _clusterMemberService
                    .Get(request.Data.Endpoint)
                    .FirstOrDefaultAsync(cancellationToken);

                if (clusterMember == null) //Handle creation
                {
                    clusterMember = await (await _clusterMemberService.CreateAsync(request.Data)).FirstOrDefaultAsync(cancellationToken);

                    if (clusterMember == null) throw new Exception("Could not create the cluster member.");
                }

                return Unit.Value;
            }
        }

        public class Validator : AbstractValidator<CreateOrUpdateClusterMemberCmd>
        {
            public Validator()
            {
                RuleFor(x => x.Data)
                    .NotNull()
                    .WithMessage("Missing data object.").DependentRules(() =>
                    {
                        RuleFor(x => x.Data.Endpoint)
                            .NotEmpty()
                                .WithMessage("Field Endpoint is required.");
                    });
            }
        }
    }
}
