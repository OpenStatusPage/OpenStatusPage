using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenStatusPage.Server.Application.Cluster.Communication;
using OpenStatusPage.Server.Application.Misc;
using OpenStatusPage.Server.Domain.Entities.Cluster;

namespace OpenStatusPage.Server.Application.Cluster.Discovery.Commands
{
    public class ClusterMembersQuery : RequestBase<ClusterMembersQuery.Response>
    {
        public QueryExtension<ClusterMember> Query { get; set; }

        public class Handler : IRequestHandler<ClusterMembersQuery, Response>
        {
            private readonly ClusterMemberService _clusterMemberService;

            public Handler(ClusterMemberService clusterMemberService)
            {
                _clusterMemberService = clusterMemberService;
            }

            public async Task<Response> Handle(ClusterMembersQuery request, CancellationToken cancellationToken)
            {
                return new Response
                {
                    ClusterMembers = await _clusterMemberService
                        .Get()
                        .Apply(request.Query)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public class Response
        {
            public List<ClusterMember> ClusterMembers { get; set; }
        }
    }
}
