using OpenStatusPage.Server.Domain.Entities.Cluster;
using OpenStatusPage.Server.Persistence;
using OpenStatusPage.Shared.Interfaces;

namespace OpenStatusPage.Server.Application.Cluster.Discovery
{
    public class ClusterMemberService : IScopedService
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public ClusterMemberService(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public IQueryable<ClusterMember> Get()
            => _applicationDbContext.ClusterMembers;

        public IQueryable<ClusterMember> Get(Uri endpoint)
            => _applicationDbContext.ClusterMembers.Where(x => x.Endpoint.Equals(endpoint));

        public async Task UpdateAsync(ClusterMember clusterMember)
            => await _applicationDbContext.SaveChangesAsync();

        public async Task DeleteAsync(ClusterMember clusterMember)
        {
            _applicationDbContext.Remove(clusterMember);

            await _applicationDbContext.SaveChangesAsync();
        }

        public async Task<IQueryable<ClusterMember>> CreateAsync(ClusterMember memberData)
        {
            var clusterMember = await _applicationDbContext.CreateEntityAsync(x => x.ClusterMembers, false);

            clusterMember.Endpoint = memberData.Endpoint;

            _applicationDbContext.Add(clusterMember);

            await _applicationDbContext.SaveChangesAsync();

            return Get(clusterMember.Endpoint);
        }
    }
}
